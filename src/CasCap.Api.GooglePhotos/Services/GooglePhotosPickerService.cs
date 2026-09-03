using CasCap.Common.Services;
using CasCap.Models.Picker;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace CasCap.Services;

/// <summary>Provides access to Google Photos Picker sessions and user-selected media.</summary>
public sealed class GooglePhotosPickerService : HttpClientBase
{
    private readonly GooglePhotosCredentialProvider _credentialProvider;

    /// <summary>Initializes a new Picker API client.</summary>
    public GooglePhotosPickerService(
        ILogger<GooglePhotosPickerService> logger,
        GooglePhotosCredentialProvider credentialProvider,
        HttpClient client)
    {
        _logger = logger;
        _credentialProvider = credentialProvider;
        Client = client;
    }

    /// <summary>Authenticates the configured user for Picker API requests.</summary>
    /// <remarks>
    /// The resulting grant is shared with every other Google Photos client through
    /// <see cref="GooglePhotosCredentialProvider" />, so authenticating once is enough.
    /// </remarks>
    public Task<bool> LoginAsync(CancellationToken cancellationToken = default)
        => _credentialProvider.LoginAsync(cancellationToken);

    /// <summary>Sets an externally acquired authorization header.</summary>
    public void SetAuth(string tokenType, string accessToken)
        => _credentialProvider.SetAuthorization(tokenType, accessToken);

    /// <summary>Creates a session in which the user can select media items.</summary>
    public async Task<PickingSession> CreateSessionAsync(
        int? maxItemCount = null,
        Guid? requestId = null,
        CancellationToken cancellationToken = default)
    {
        if (maxItemCount is < 0 or > 2000)
            throw new ArgumentOutOfRangeException(nameof(maxItemCount), "Picker sessions support values between 0 and 2000.");
        if (requestId is { } id && id.ToString("D")[14] != '4')
            throw new ArgumentException("Picker request ID must be a UUID version 4.", nameof(requestId));

        var requestUri = requestId.HasValue
            ? QueryHelpers.AddQueryString(PickerRequestUris.Sessions, "requestId", requestId.Value.ToString("D"))
            : PickerRequestUris.Sessions;
        var request = new
        {
            pickingConfig = maxItemCount.HasValue
                ? new PickerPickingConfig { MaxItemCount = maxItemCount.Value.ToString(CultureInfo.InvariantCulture) }
                : null
        };
        var response = await PostJson<PickingSession, Error>(requestUri, request, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.error is not null)
            throw new GooglePhotosException(response.error);

        return response.result ?? throw new GooglePhotosException("Picker session creation returned no session.");
    }

    /// <summary>Gets the current state of a Picker session.</summary>
    public async Task<PickingSession> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var response = await Get<PickingSession, Error>($"{PickerRequestUris.Sessions}/{Uri.EscapeDataString(sessionId)}", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (response.error is not null)
            throw new GooglePhotosException(response.error);

        return response.result ?? throw new GooglePhotosException("Picker session lookup returned no session.");
    }

    /// <summary>Deletes a Picker session.</summary>
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{PickerRequestUris.Sessions}/{Uri.EscapeDataString(sessionId)}");
        using var response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (responseBody.TryFromJson<Error>(out var error) && error is not null)
            throw new GooglePhotosException(error);

        throw new GooglePhotosException($"Picker session deletion failed with HTTP {(int)response.StatusCode}.");
    }

    /// <summary>Lists media items selected during a completed Picker session.</summary>
    public async IAsyncEnumerable<PickedMediaItem> GetMediaItemsAsync(
        string sessionId,
        int pageSize = 50,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (pageSize is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Picker page size must be between 1 and 100.");

        string? pageToken = null;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = new Dictionary<string, string?>
            {
                ["sessionId"] = sessionId,
                ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
                ["pageToken"] = pageToken
            };
            var requestUri = QueryHelpers.AddQueryString(PickerRequestUris.MediaItems, query);
            var response = await Get<PickerMediaItemsResponse, Error>(requestUri, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (response.error is not null)
                throw new GooglePhotosException(response.error);

            var page = response.result ?? throw new GooglePhotosException("Picker media lookup returned no response.");
            foreach (var mediaItem in page.MediaItems)
                yield return mediaItem;

            pageToken = page.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));
    }

    /// <summary>Streams a selected photo to the destination stream.</summary>
    public Task DownloadPhotoAsync(
        PickedMediaItem mediaItem,
        Stream destination,
        int maxWidth,
        int maxHeight,
        bool includeExifMetadata = false,
        CancellationToken cancellationToken = default)
    {
        if (maxWidth is < 1 or > 16383)
            throw new ArgumentOutOfRangeException(nameof(maxWidth), maxWidth, "Picker photo width must be between 1 and 16383.");
        if (maxHeight is < 1 or > 16383)
            throw new ArgumentOutOfRangeException(nameof(maxHeight), maxHeight, "Picker photo height must be between 1 and 16383.");

        var parameters = $"w{maxWidth}-h{maxHeight}{(includeExifMetadata ? "-d" : string.Empty)}";
        return DownloadMediaAsync(mediaItem, destination, parameters, cancellationToken);
    }

    /// <summary>Streams a selected video to the destination stream.</summary>
    public Task DownloadVideoAsync(PickedMediaItem mediaItem, Stream destination, CancellationToken cancellationToken = default)
        => DownloadMediaAsync(mediaItem, destination, "dv", cancellationToken);

    private async Task DownloadMediaAsync(
        PickedMediaItem mediaItem,
        Stream destination,
        string parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediaItem);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaItem.MediaFile.BaseUrl);

        using var response = await Client.GetAsync(
            $"{mediaItem.MediaFile.BaseUrl}={parameters}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (responseBody.TryFromJson<Error>(out var error) && error is not null)
                throw new GooglePhotosException(error);

            throw new GooglePhotosException($"Picker media download failed with HTTP {(int)response.StatusCode}.");
        }

        await response.Content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }
}
