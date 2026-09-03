using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.Collections.Frozen;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CasCap.Services;

/// <summary>Creates and refreshes OAuth credentials for Google Photos API clients.</summary>
internal static class GooglePhotosAuthorization
{
    private static readonly FrozenDictionary<GooglePhotosScope, string> Scopes = new Dictionary<GooglePhotosScope, string>
    {
        { GooglePhotosScope.AppendOnly, "https://www.googleapis.com/auth/photoslibrary.appendonly" },
        { GooglePhotosScope.ReadOnlyAppCreatedData, "https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata" },
        { GooglePhotosScope.EditAppCreatedData, "https://www.googleapis.com/auth/photoslibrary.edit.appcreateddata" },
        { GooglePhotosScope.PickerMediaItemsReadOnly, "https://www.googleapis.com/auth/photospicker.mediaitems.readonly" }
    }.ToFrozenDictionary();

    /// <summary>Authorizes the configured user and returns an HTTP authorization header.</summary>
    internal static async Task<AuthenticationHeaderValue?> AuthorizeAsync(
        ILogger logger,
        GooglePhotosOptions options,
        CancellationToken cancellationToken)
    {
        var secrets = new ClientSecrets
        {
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret
        };

        FileDataStore? dataStore = null;
        if (!string.IsNullOrWhiteSpace(options.FileDataStoreFullPathOverride))
            dataStore = new FileDataStore(options.FileDataStoreFullPathOverride, true);

        var requestedScopes = GetScopes(options.Scopes);
        logger.LogDebug("{ClassName} requesting authorization", nameof(GooglePhotosAuthorization));
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            requestedScopes,
            GetTokenStoreKey(options.User, requestedScopes),
            cancellationToken,
            dataStore).ConfigureAwait(false);

        if (credential.Token.IsStale)
        {
            logger.LogWarning("{ClassName} access token expired; refreshing it", nameof(GooglePhotosAuthorization));
            if (!await credential.RefreshTokenAsync(cancellationToken).ConfigureAwait(false))
            {
                logger.LogError("{ClassName} failed to refresh the access token", nameof(GooglePhotosAuthorization));
                return null;
            }
        }

        if (!string.IsNullOrWhiteSpace(credential.Token.Scope))
        {
            var grantedScopes = credential.Token.Scope
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToFrozenSet(StringComparer.Ordinal);
            if (requestedScopes.Any(scope => !grantedScopes.Contains(scope)))
                throw new GooglePhotosException("The OAuth grant does not include all configured Google Photos scopes. Authenticate again and approve every requested scope.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(credential.Token.TokenType);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential.Token.AccessToken);
        return new AuthenticationHeaderValue(credential.Token.TokenType, credential.Token.AccessToken);
    }

    private static string[] GetScopes(IEnumerable<GooglePhotosScope> scopes)
        => scopes.Select(scope => Scopes[scope]).ToArray();

    private static string GetTokenStoreKey(string user, IEnumerable<string> scopes)
    {
        var normalizedScopes = string.Join('\n', scopes.Order(StringComparer.Ordinal));
        var source = Encoding.UTF8.GetBytes($"{user}\n{normalizedScopes}");
        return Convert.ToHexString(SHA256.HashData(source));
    }
}