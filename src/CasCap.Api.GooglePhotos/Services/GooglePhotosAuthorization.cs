using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.Collections.Frozen;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CasCap.Services;

/// <summary>Creates and refreshes OAuth credentials for Google Photos API clients.</summary>
internal static partial class GooglePhotosAuthorization
{
    private static readonly FrozenDictionary<GooglePhotosScope, string> Scopes = new Dictionary<GooglePhotosScope, string>
    {
        { GooglePhotosScope.AppendOnly, "https://www.googleapis.com/auth/photoslibrary.appendonly" },
        { GooglePhotosScope.ReadOnlyAppCreatedData, "https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata" },
        { GooglePhotosScope.EditAppCreatedData, "https://www.googleapis.com/auth/photoslibrary.edit.appcreateddata" },
        { GooglePhotosScope.PickerMediaItemsReadOnly, "https://www.googleapis.com/auth/photospicker.mediaitems.readonly" }
    }.ToFrozenDictionary();

    /// <summary>Authorizes the configured user and returns the resulting Google credential.</summary>
    internal static async Task<UserCredential?> AuthorizeAsync(
        ILogger logger,
        GooglePhotosOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.User)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(options.User)} cannot be null!");
        if (string.IsNullOrWhiteSpace(options.ClientId)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(options.ClientId)} cannot be null!");
        if (string.IsNullOrWhiteSpace(options.ClientSecret)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(options.ClientSecret)} cannot be null!");
        if (options.Scopes.IsNullOrEmpty()) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(options.Scopes)} cannot be null/empty!");

        var secrets = new ClientSecrets
        {
            ClientId = options.ClientId,
            ClientSecret = options.ClientSecret
        };

        FileDataStore? dataStore = null;
        if (!string.IsNullOrWhiteSpace(options.FileDataStoreFullPathOverride))
            dataStore = new FileDataStore(options.FileDataStoreFullPathOverride, true);

        var requestedScopes = GetScopes(options.Scopes);
        LogRequestingAuthorization(logger, nameof(GooglePhotosAuthorization));
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            requestedScopes,
            GetTokenStoreKey(options.User, options.ClientId, requestedScopes),
            cancellationToken,
            dataStore).ConfigureAwait(false);

        if (credential.Token.IsStale)
        {
            LogRefreshingAccessToken(logger, nameof(GooglePhotosAuthorization));
            if (!await credential.RefreshTokenAsync(cancellationToken).ConfigureAwait(false))
            {
                LogAccessTokenRefreshFailed(logger, nameof(GooglePhotosAuthorization));
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

        return credential;
    }

    private static string[] GetScopes(IEnumerable<GooglePhotosScope> scopes)
        => scopes.Select(scope => Scopes.TryGetValue(scope, out var value)
            ? value
            : throw new GooglePhotosException($"Unsupported Google Photos OAuth scope: {scope}.")).ToArray();

    /// <summary>Creates an OAuth cache key isolated by local user, OAuth client, and requested scopes.</summary>
    internal static string GetTokenStoreKey(string user, string clientId, IEnumerable<string> scopes)
    {
        var normalizedScopes = string.Join('\n', scopes.Order(StringComparer.Ordinal));
        var source = Encoding.UTF8.GetBytes($"{user}\n{clientId}\n{normalizedScopes}");
        return Convert.ToHexString(SHA256.HashData(source));
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{ClassName} requesting authorization")]
    private static partial void LogRequestingAuthorization(ILogger logger, string className);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{ClassName} access token expired; refreshing it")]
    private static partial void LogRefreshingAccessToken(ILogger logger, string className);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "{ClassName} failed to refresh the access token")]
    private static partial void LogAccessTokenRefreshFailed(ILogger logger, string className);
}
