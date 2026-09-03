using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.Collections.Frozen;
using System.Net.Http.Headers;

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

        logger.LogDebug("{ClassName} requesting authorization", nameof(GooglePhotosAuthorization));
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            GetScopes(options.Scopes),
            options.User,
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

        ArgumentException.ThrowIfNullOrWhiteSpace(credential.Token.TokenType);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential.Token.AccessToken);
        return new AuthenticationHeaderValue(credential.Token.TokenType, credential.Token.AccessToken);
    }

    private static string[] GetScopes(IEnumerable<GooglePhotosScope> scopes)
        => scopes.Select(scope => Scopes[scope]).ToArray();
}