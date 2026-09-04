using CasCap.Common.Services;
using Microsoft.AspNetCore.WebUtilities;
using MimeTypes;
using System.Buffers;
using System.Collections.Frozen;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Web;

namespace CasCap.Services;

/// <summary>Provides authentication, album, media item, enrichment, and upload operations for the Google Photos Library API.</summary>
public abstract partial class GooglePhotosServiceBase : HttpClientBase
{
    private const int maxSizeImageBytes = 1024 * 1024 * 200;
    private const long maxSizeVideoBytes = 1024 * 1024 * 1024 * 20L;

    private const int minPageSizeAlbums = 1;
    private const int defaultPageSizeAlbums = 50;
    private const int maxPageSizeAlbums = 50;

    private const int minPageSizeMediaItems = 1;
    private const int defaultPageSizeMediaItems = 100;
    private const int maxPageSizeMediaItems = 100;

    private const int defaultBatchSizeMediaItems = 50;

    private readonly GooglePhotosCredentialProvider _credentialProvider;
    private readonly IOptions<GooglePhotosOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosServiceBase" /> class.</summary>
    /// <param name="logger">The logger used for request diagnostics.</param>
    /// <param name="options">The configured Google Photos options.</param>
    /// <param name="credentialProvider">The authorization shared by every Google Photos client.</param>
    /// <param name="client">The HTTP client used to send API requests.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client" /> is <see langword="null" />.</exception>
    protected GooglePhotosServiceBase(
        ILogger<GooglePhotosServiceBase> logger,
        IOptions<GooglePhotosOptions> options,
        GooglePhotosCredentialProvider credentialProvider,
        HttpClient client)
    {
        _logger = logger;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        Client = client ?? throw new ArgumentNullException(nameof(client), $"{nameof(HttpClient)} cannot be null!");
    }

    /// <summary>Raises <see cref="PagingEvent" /> after a page of results is processed.</summary>
    /// <param name="args">The page progress information.</param>
    protected virtual void RaisePagingEvent(PagingEventArgs args) => PagingEvent?.Invoke(this, args);

    /// <summary>Occurs after a page of results is processed and another page is available.</summary>
    public event EventHandler<PagingEventArgs>? PagingEvent;

    /// <summary>Raises <see cref="UploadProgressEvent" /> after an upload chunk is accepted.</summary>
    /// <param name="args">The upload progress information.</param>
    protected virtual void RaiseUploadProgressEvent(UploadProgressEventArgs args) => UploadProgressEvent?.Invoke(this, args);

    /// <summary>Occurs after a resumable upload chunk is accepted.</summary>
    public event EventHandler<UploadProgressEventArgs>? UploadProgressEvent;

    /// <summary>Determines whether a file has an image or video extension supported by Google Photos.</summary>
    /// <param name="path">The file path to inspect.</param>
    /// <returns><see langword="true" /> when the file extension is supported; otherwise, <see langword="false" />.</returns>
    public static bool IsFileUploadable(string path) => IsFileUploadableByExtension(Path.GetExtension(path));

    /// <summary>Determines whether an extension maps to an image or video MIME type supported by Google Photos.</summary>
    /// <param name="extension">The file extension to inspect.</param>
    /// <returns><see langword="true" /> when the extension is supported; otherwise, <see langword="false" />.</returns>
    public static bool IsFileUploadableByExtension(string extension) => IsImage(extension) || IsVideo(extension);

    private static bool IsImage(string extension) => AcceptedMimeTypesImage.Contains(MimeTypeMap.GetMimeType(extension));

    //https://developers.google.com/photos/library/guides/upload-media#file-types-sizes
    private static readonly FrozenSet<string> AcceptedMimeTypesImage = new[]
    {
        "image/avif",
        "image/bmp",
        "image/gif",
        "image/heic",
        "image/vnd.microsoft.icon",
        "image/jpg",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "image/webp",
        "image/x-panasonic-raw",
        "image/x-panasonic-rw2",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static bool IsVideo(string extension) => AcceptedMimeTypesVideo.Contains(MimeTypeMap.GetMimeType(extension));

    private static readonly FrozenSet<string> AcceptedMimeTypesVideo = new[]
    {
        "video/3gpp",
        "video/3gpp2",
        "video/x-ms-asf",
        "video/x-msvideo",
        "video/divx",
        "video/mpeg",//https://en.wikipedia.org/wiki/MPEG_transport_stream
        "video/mp4",
        "video/mp2t",
        "video/x-m4v",
        "video/x-matroska",
        "video/mmv",
        "video/mod",
        "video/quicktime",
        "video/x-ms-wmv",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Authenticates with the configured options so that subsequent requests are authorized.</summary>
    /// <param name="cancellationToken">A token that can cancel authentication.</param>
    /// <returns><see langword="true" /> when authorization succeeds; otherwise, <see langword="false" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when required settings are missing or a scope is unsupported.</exception>
    /// <remarks>
    /// The resulting grant is held by <see cref="GooglePhotosCredentialProvider" /> and applied per request, so it is
    /// shared by every resolved client and its access token is refreshed automatically.
    /// </remarks>
    public Task<bool> LoginAsync(CancellationToken cancellationToken = default)
        => _credentialProvider.LoginAsync(cancellationToken);

    /// <summary>
    /// Workaround to allow setting the auth header when running integration tests from CI.
    /// </summary>
    /// <param name="tokenType">The authorization scheme, such as <c>Bearer</c>.</param>
    /// <param name="accessToken">The access token value.</param>
    public void SetAuth(string tokenType, string accessToken)
        => _credentialProvider.SetAuthorization(tokenType, accessToken);

    #region https://photoslibrary.googleapis.com/v1/albums

    //https://photoslibrary.googleapis.com/v1/albums/{albumId}
    /// <summary>Retrieves an album by its identifier.</summary>
    /// <param name="albumId">The album identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The album when found; otherwise, <see langword="null" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default)
    {
        var tpl = await Get<Album, Error>(string.Format(RequestUris.GET_album, albumId), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tpl.error is not null)
            throw new GooglePhotosException(tpl.error);

        return tpl.result;
    }

    /// <summary>Lists albums created by this application.</summary>
    /// <param name="pageSize">The number of albums to request per page, from 1 through 50.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>All albums returned by the API.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageSize" /> is outside the supported range.</exception>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public Task<List<Album>> GetAlbumsAsync(int pageSize = defaultPageSizeAlbums, CancellationToken cancellationToken = default)
        => GetAlbumsPagedAsync(RequestUris.GET_albums, pageSize, cancellationToken);

    //TODO: GetAlbumsPagedAsync, GetMediaItemsPagedAsync and SearchMediaItemsPagedAsync each repeat the same
    //continuation-token walk. Give the paged responses a shared IPagingToken-based abstraction and write it once.
    private async Task<List<Album>> GetAlbumsPagedAsync(string requestUri, int pageSize, CancellationToken cancellationToken)
    {
        if (pageSize < minPageSizeAlbums || pageSize > maxPageSizeAlbums)
            throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeAlbums} and {maxPageSizeAlbums}!");

        var l = new List<Album>();
        var pageToken = string.Empty;
        var pageNumber = 1;
        while (pageToken is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var _requestUri = GetUrl(requestUri, pageSize, pageToken);
            var tpl = await Get<AlbumsGetResponse, Error>(_requestUri, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
            else if (tpl.result is not null)//to hide nullability warning
            {
                var batch = new List<Album>(pageSize);
                if (!tpl.result.Albums.IsNullOrEmpty()) batch = tpl.result.Albums ?? [];
                l.AddRange(batch);
                if (!string.IsNullOrWhiteSpace(tpl.result.NextPageToken))
                    RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, l.Count));
                pageToken = tpl.result.NextPageToken;
                pageNumber++;
            }
            else
                break;
        }
        return l;
    }

    private string GetUrl(string uri, int pageSize, string? pageToken)
    {
        var queryParams = new Dictionary<string, string?>(2)
        {
            [nameof(pageSize)] = pageSize.ToString(CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(pageToken))
            queryParams[nameof(pageToken)] = pageToken;
        var url = QueryHelpers.AddQueryString(uri, queryParams);
        LogRequestUrl(_logger, nameof(GooglePhotosServiceBase), nameof(GetUrl), url);
        return url;
    }

    /// <summary>Creates an album with the specified title.</summary>
    /// <param name="title">The title of the new album.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created album when returned by the API; otherwise, <see langword="null" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<Album?> CreateAlbumAsync(string title, CancellationToken cancellationToken = default)
    {
        var req = new { album = new Album { Title = title } };
        var tpl = await PostJson<Album, Error>(RequestUris.POST_albums, req, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result;
    }

    /// <summary>Adds media items to an album in API-sized batches.</summary>
    /// <param name="albumId">The destination album identifier.</param>
    /// <param name="mediaItemIds">The media item identifiers to add.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns><see langword="true" /> when all batches are accepted.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public Task<bool> AddMediaItemsToAlbumAsync(string albumId, string[] mediaItemIds, CancellationToken cancellationToken = default)
        => AddMediaItemsToAlbumAsync(albumId, mediaItemIds.ToList(), cancellationToken);

    /// <summary>Adds media items to an album in API-sized batches.</summary>
    /// <param name="albumId">The destination album identifier.</param>
    /// <param name="mediaItemIds">The media item identifiers to add.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns><see langword="true" /> when all batches are accepted.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<bool> AddMediaItemsToAlbumAsync(string albumId, List<string> mediaItemIds, CancellationToken cancellationToken = default)
    {
        var batches = mediaItemIds.Distinct().ToList().GetBatches(defaultBatchSizeMediaItems);
        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = new { mediaItemIds = batch.Value };
            var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchAddMediaItems, albumId), req, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        }
        return true;
    }

    /// <summary>Removes media items from an album in API-sized batches.</summary>
    /// <param name="albumId">The album identifier.</param>
    /// <param name="mediaItemIds">The media item identifiers to remove.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns><see langword="true" /> when all batches are accepted.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, string[] mediaItemIds, CancellationToken cancellationToken = default)
        => RemoveMediaItemsFromAlbumAsync(albumId, mediaItemIds.ToList(), cancellationToken);

    /// <summary>Removes media items from an album in API-sized batches.</summary>
    /// <param name="albumId">The album identifier.</param>
    /// <param name="mediaItemIds">The media item identifiers to remove.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns><see langword="true" /> when all batches are accepted.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, List<string> mediaItemIds, CancellationToken cancellationToken = default)
    {
        var batches = mediaItemIds.GetBatches(defaultBatchSizeMediaItems);
        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = new { mediaItemIds = batch.Value };
            var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchRemoveMediaItems, albumId), req, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        }
        return true;
    }

    /// <summary>Adds an enrichment item at a specified position in an album.</summary>
    /// <param name="albumId">The album identifier.</param>
    /// <param name="newEnrichmentItem">The enrichment content to add.</param>
    /// <param name="albumPosition">The position at which to add the enrichment.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The created enrichment item when returned by the API; otherwise, <see langword="null" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<EnrichmentItem?> AddEnrichmentToAlbumAsync(string albumId, NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition, CancellationToken cancellationToken = default)
    {
        var tpl = await PostJson<AddEnrichmentResponse, Error>(string.Format(RequestUris.POST_albums_addEnrichment, albumId), new AddEnrichmentRequest(newEnrichmentItem, albumPosition), cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result?.EnrichmentItem;
    }

    #endregion

    #region https://photoslibrary.googleapis.com/v1/mediaItems
    //TODO: identical to SearchMediaItemsPagedAsync apart from the GET/POST request; see the paging note above.
    private async IAsyncEnumerable<MediaItem> GetMediaItemsPagedAsync(int pageSize, int maxPageCount, string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (pageSize < minPageSizeMediaItems || pageSize > maxPageSizeMediaItems)
            throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeMediaItems} and {maxPageSizeMediaItems}!");

        //Note: MediaItem results are not guaranteed to be unique so we check returned ids in a volatile HashSet
        var hs = new HashSet<string>();
        var pageToken = string.Empty;
        var pageNumber = 1;
        while (pageToken is not null && pageNumber <= maxPageCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var _requestUri = GetUrl(requestUri, pageSize, pageToken);
            var tpl = await Get<MediaItemsResponse, Error>(_requestUri, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
            else if (tpl.result is not null)
            {
                var batch = new List<MediaItem>(pageSize);
                if (!tpl.result.MediaItems.IsNullOrEmpty()) batch = tpl.result.MediaItems ?? [];
                foreach (var mi in batch)
                    if (!hs.Contains(mi.Id))
                    {
                        hs.Add(mi.Id);
                        yield return mi;
                    }
                if (!string.IsNullOrWhiteSpace(tpl.result.NextPageToken) && batch.Count != 0)
                {
                    //Note: low page sizes can return 0 records but still return a continuation token, weirdness
                    RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, hs.Count)
                    {
                        MinDate = batch.Min(p => p.MediaMetadata.CreationTime),
                        MaxDate = batch.Max(p => p.MediaMetadata.CreationTime),
                    });
                }
                pageToken = tpl.result.NextPageToken;
                pageNumber++;
            }
            else
                break;
        }
    }

    //TODO: see the paging note above.
    private async IAsyncEnumerable<MediaItem> SearchMediaItemsPagedAsync(string? albumId, int pageSize, int maxPageCount, Filter? filters, string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (pageSize < minPageSizeMediaItems || pageSize > maxPageSizeMediaItems)
            throw new ArgumentOutOfRangeException($"{nameof(pageSize)} must be between {minPageSizeMediaItems} and {maxPageSizeMediaItems}!");

        //Note: mediaitem results are not garuanteed to be unique so we check returned ids in a volatile hashset
        var hs = new HashSet<string>();
        var pageToken = string.Empty;
        var pageNumber = 1;
        while (pageToken is not null && pageNumber <= maxPageCount)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = new { albumId, pageSize, pageToken, filters };
            var tpl = await PostJson<MediaItemsResponse, Error>(requestUri, req, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
            else if (tpl.result is not null)
            {
                var batch = new List<MediaItem>(pageSize);
                if (!tpl.result.MediaItems.IsNullOrEmpty()) batch = tpl.result.MediaItems ?? [];
                foreach (var mi in batch)
                    if (!hs.Contains(mi.Id))
                    {
                        hs.Add(mi.Id);
                        yield return mi;
                    }
                if (!string.IsNullOrWhiteSpace(tpl.result.NextPageToken) && batch.Count != 0)
                    RaisePagingEvent(new PagingEventArgs(batch.Count, pageNumber, hs.Count)
                    {
                        MinDate = batch.Min(p => p.MediaMetadata.CreationTime),
                        MaxDate = batch.Max(p => p.MediaMetadata.CreationTime),
                    });
                pageToken = tpl.result.NextPageToken;
                pageNumber++;
            }
            else
                break;
        }
    }

    /// <summary>Streams media items from the user's library.</summary>
    /// <param name="pageSize">The number of media items to request per page, from 1 through 100.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of unique media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsAsync(int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsPagedAsync(pageSize, maxPageCount, RequestUris.GET_mediaItems, cancellationToken);

    /// <summary>Streams media items contained in an album.</summary>
    /// <param name="albumId">The album identifier.</param>
    /// <param name="pageSize">The number of media items to request per page, from 1 through 100.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of unique media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByAlbumAsync(string albumId, int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => SearchMediaItemsPagedAsync(albumId, pageSize, maxPageCount, null, RequestUris.POST_mediaItems_search, cancellationToken);

    //https://photoslibrary.googleapis.com/v1/mediaItems/media-item-id
    /// <summary>Retrieves a media item by its identifier.</summary>
    /// <param name="mediaItemId">The media item identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The media item when found; otherwise, <see langword="null" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async Task<MediaItem?> GetMediaItemByIdAsync(string mediaItemId, CancellationToken cancellationToken = default)
    {
        var tpl = await Get<MediaItem, Error>($"{RequestUris.GET_mediaItems}/{mediaItemId}", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result;
    }

    //https://photoslibrary.googleapis.com/v1/mediaItems:batchGet?mediaItemIds=media-item-id&mediaItemIds=another-media-item-id&mediaItemIds=incorrect-media-item-id
    /// <summary>Streams media items matching the supplied identifiers.</summary>
    /// <param name="mediaItemIds">The media item identifiers to retrieve.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of unique media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByIdsAsync(string[] mediaItemIds, CancellationToken cancellationToken = default)
        => GetMediaItemsByIdsAsync(mediaItemIds.ToList(), cancellationToken);

    /// <summary>Streams media items matching the supplied identifiers.</summary>
    /// <param name="mediaItemIds">The media item identifiers to retrieve.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of unique media items.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public async IAsyncEnumerable<MediaItem> GetMediaItemsByIdsAsync(List<string> mediaItemIds, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var hs = new HashSet<string>();
        var batches = mediaItemIds.GetBatches(defaultBatchSizeMediaItems);
        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            //see https://github.com/dotnet/aspnetcore/issues/7945 can't use QueryHelpers.AddQueryString
            //var queryParams = new Dictionary<string, string>(batch.Value.Length);
            //foreach (var mediaItemId in batch.Value)
            //    queryParams.Add(nameof(mediaItemIds), mediaItemId);
            //var url = QueryHelpers.AddQueryString(RequestUris.GET_mediaItems_batchGet, queryParams);
            var sb = new StringBuilder();
            foreach (var mediaItemId in batch.Value)
                sb.Append($"&{nameof(mediaItemIds)}={mediaItemId}");
            var url = $"{RequestUris.GET_mediaItems_batchGet}?{sb.ToString()[1..]}";
            var tpl = await Get<MediaItemsGetResponse, Error>(url, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
            else if (tpl.result is not null)
            {
                foreach (var result in tpl.result.MediaItemResults)
                {
                    if (result.Status is null)
                    {
                        if (!hs.Contains(result.MediaItem.Id))
                        {
                            hs.Add(result.MediaItem.Id);
                            yield return result.MediaItem;
                        }
                    }
                    else
                        LogMediaItemStatus(
                            _logger,
                            nameof(GooglePhotosServiceBase),
                            nameof(GetMediaItemsByIdsAsync),
                            result.Status.Code,
                            result.Status.StatusName);//we highlight if any objects returned a non-null status object
                }
                if (batch.Key + 1 != batches.Count)
                    RaisePagingEvent(new PagingEventArgs(tpl.result.MediaItemResults.Count, batch.Key + 1, hs.Count));
            }
        }
    }

    /// <summary>Streams media items created within an inclusive date range.</summary>
    /// <param name="startDate">The beginning of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of matching media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByDateRangeAsync(DateTime startDate, DateTime endDate, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(startDate, endDate), maxPageCount, cancellationToken);

    /// <summary>Streams media items matching a content category.</summary>
    /// <param name="category">The content category to include.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of matching media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoryAsync(GooglePhotosContentCategoryType category, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(category), maxPageCount, cancellationToken);

    /// <summary>Streams media items matching any supplied content category.</summary>
    /// <param name="categories">The content categories to include.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of matching media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoriesAsync(GooglePhotosContentCategoryType[] categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

    /// <summary>Streams media items matching any supplied content category.</summary>
    /// <param name="categories">The content categories to include.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of matching media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoriesAsync(List<GooglePhotosContentCategoryType> categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

    /// <summary>Streams media items matching the supplied search filter.</summary>
    /// <param name="filter">The filter applied to the media item search.</param>
    /// <param name="maxPageCount">The maximum number of pages to request.</param>
    /// <param name="cancellationToken">A token that can cancel enumeration.</param>
    /// <returns>An asynchronous sequence of matching media items.</returns>
    public IAsyncEnumerable<MediaItem> GetMediaItemsByFilterAsync(Filter filter, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => SearchMediaItemsByFilterAsync(filter, maxPageCount, cancellationToken);

    private IAsyncEnumerable<MediaItem> SearchMediaItemsByFilterAsync(Filter filter, int maxPageCount, CancellationToken cancellationToken)
    {
        //validate/tidy outgoing filter object
        var contentFilter = filter.ContentFilter;
        if (contentFilter is not null)
        {
            if (contentFilter.IncludedContentCategories.IsNullOrEmpty()) contentFilter.IncludedContentCategories = null;
            if (contentFilter.ExcludedContentCategories.IsNullOrEmpty()) contentFilter.ExcludedContentCategories = null;
            if (contentFilter.IncludedContentCategories is null && contentFilter.ExcludedContentCategories is null)
            {
                LogEmptyFilterRemoved(_logger, nameof(GooglePhotosServiceBase), nameof(contentFilter));
                filter.ContentFilter = null;
            }
        }
        var dateFilter = filter.DateFilter;
        if (dateFilter is not null)
        {
            if (dateFilter.Dates.IsNullOrEmpty()) dateFilter.Dates = null;
            if (dateFilter.Ranges.IsNullOrEmpty()) dateFilter.Ranges = null;
            if (dateFilter.Dates is null && dateFilter.Ranges is null)
            {
                LogEmptyFilterRemoved(_logger, nameof(GooglePhotosServiceBase), nameof(dateFilter));
                filter.DateFilter = null;
            }
            //do we need to validate start/end date ranges, i.e. start before end...?
        }
        var mediaTypeFilter = filter.MediaTypeFilter;
        if (mediaTypeFilter is not null && mediaTypeFilter.MediaTypes.IsNullOrEmpty())
        {
            LogEmptyFilterRemoved(_logger, nameof(GooglePhotosServiceBase), nameof(mediaTypeFilter));
            filter.MediaTypeFilter = null;
        }

        var featureFilter = filter.FeatureFilter;
        if (featureFilter is not null && featureFilter.IncludedFeatures.IsNullOrEmpty())
        {
            LogEmptyFilterRemoved(_logger, nameof(GooglePhotosServiceBase), nameof(featureFilter));
            filter.FeatureFilter = null;
        }

        return SearchMediaItemsPagedAsync(null, defaultPageSizeMediaItems, maxPageCount, filter, RequestUris.POST_mediaItems_search, cancellationToken);
    }

    //would need renaming if made public
    private Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null, AlbumPosition? albumPosition = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, albumPosition, cancellationToken);

    /// <summary>Creates one media item from an upload token.</summary>
    /// <param name="uploadToken">The token returned by a completed media upload.</param>
    /// <param name="fileName">The optional filename sent to Google Photos.</param>
    /// <param name="description">The optional media item description.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="positionType">The requested position within the destination album.</param>
    /// <param name="relativeMediaItemId">The media item used as a relative position anchor.</param>
    /// <param name="relativeEnrichmentItemId">The enrichment item used as a relative position anchor.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The creation result when returned by the API; otherwise, <see langword="null" />.</returns>
    public Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    /// <summary>Creates one media item from prepared upload metadata.</summary>
    /// <param name="uploadItem">The upload token and media item metadata.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="positionType">The requested position within the destination album.</param>
    /// <param name="relativeMediaItemId">The media item used as a relative position anchor.</param>
    /// <param name="relativeEnrichmentItemId">The enrichment item used as a relative position anchor.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The creation result when returned by the API; otherwise, <see langword="null" />.</returns>
    public Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(uploadItem, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    //would need renaming if made public
    private async Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId, AlbumPosition? albumPosition, CancellationToken cancellationToken)
    {
        var newMediaItems = new List<UploadItem> { uploadItem };
        var res = await AddMediaItemsAsync(newMediaItems, albumId, albumPosition, cancellationToken).ConfigureAwait(false);
        if (res is not null && !res.NewMediaItemResults.IsNullOrEmpty())
            return res.NewMediaItemResults[0];
        else
        {
            LogMediaItemCreationFailed(_logger, nameof(GooglePhotosServiceBase), nameof(AddMediaItemAsync));
            return null;
        }
    }

    /// <summary>Creates media items in a batch from upload tokens and filenames.</summary>
    /// <param name="items">The upload tokens and filenames to create.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="positionType">The requested position within the destination album.</param>
    /// <param name="relativeMediaItemId">The media item used as a relative position anchor.</param>
    /// <param name="relativeEnrichmentItemId">The enrichment item used as a relative position anchor.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The batch creation response when returned by the API; otherwise, <see langword="null" />.</returns>
    public Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<(string uploadToken, string FileName)> items, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
    {
        var uploadItems = new List<UploadItem>(items.Count);
        foreach (var item in items)
            uploadItems.Add(new UploadItem(item.uploadToken, item.FileName));
        return AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);
    }

    /// <summary>Creates media items in a batch from prepared upload metadata.</summary>
    /// <param name="uploadItems">The upload tokens and media item metadata to create.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="positionType">The requested position within the destination album.</param>
    /// <param name="relativeMediaItemId">The media item used as a relative position anchor.</param>
    /// <param name="relativeEnrichmentItemId">The enrichment item used as a relative position anchor.</param>
    /// <param name="cancellationToken">A token that can cancel the request.</param>
    /// <returns>The batch creation response when returned by the API; otherwise, <see langword="null" />.</returns>
    public Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    //would need renaming if made public
    private async Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId, AlbumPosition? albumPosition, CancellationToken cancellationToken)
    {
        if (uploadItems.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(uploadItems), $"Invalid {nameof(uploadItems)} quantity, must be >= 1");
        if (albumPosition is not null && uploadItems.Count > defaultBatchSizeMediaItems)
            throw new NotSupportedException("Explicit album positioning supports at most 50 media items per request.");

        var response = new MediaItemsCreateResponse { NewMediaItemResults = [] };
        foreach (var batch in uploadItems.Chunk(defaultBatchSizeMediaItems))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var newMediaItems = new List<NewMediaItem>(batch.Length);
            foreach (var mediaItem in batch)
            {
                newMediaItems.Add(new NewMediaItem
                {
                    Description = mediaItem.Description,
                    SimpleMediaItem = new SimpleMediaItem
                    {
                        FileName = mediaItem.FileName,
                        UploadToken = mediaItem.UploadToken,
                    }
                });
            }

            var request = new { newMediaItems, albumId, albumPosition };
            var result = await PostJson<MediaItemsCreateResponse, Error>(RequestUris.POST_mediaItems_batchCreate, request, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.error is not null)
                throw new GooglePhotosException(result.error);
            if (result.result is not null)
                response.NewMediaItemResults.AddRange(result.result.NewMediaItemResults);
        }

        return response;
    }
    #endregion

    private const string X_Goog_Upload_Content_Type = UploadHeaders.ContentType;
    private const string X_Goog_Upload_Protocol = UploadHeaders.Protocol;
    private const string X_Goog_Upload_Command = UploadHeaders.Command;
    private const string X_Goog_Upload_File_Name = UploadHeaders.FileName;
    private const string X_Goog_Upload_Raw_Size = UploadHeaders.RawSize;
    private const string X_Goog_Upload_URL = UploadHeaders.Url;
    private const string X_Goog_Upload_Offset = UploadHeaders.Offset;
    private const string X_Goog_Upload_Status = UploadHeaders.Status;
    private const string X_Goog_Upload_Chunk_Granularity = UploadHeaders.ChunkGranularity;
    private const string X_Goog_Upload_Size_Received = UploadHeaders.SizeReceived;

    //TODO: this runs all three upload protocols through one method with shared mutable state, which makes it the
    //least readable code in the library. Split it per GooglePhotosUploadMethod behind a common return contract.
    //https://developers.google.com/photos/library/guides/upload-media
    //https://developers.google.com/photos/library/guides/upload-media#uploading-bytes
    //https://developers.google.com/photos/library/guides/resumable-uploads
    /// <summary>Uploads a supported image or video and returns the token used to create a media item.</summary>
    /// <param name="path">The local path of the media file to upload.</param>
    /// <param name="uploadMethod">The upload protocol to use.</param>
    /// <param name="callback">An optional upload callback retained for API compatibility.</param>
    /// <param name="cancellationToken">A token that can cancel the upload.</param>
    /// <returns>The upload token when the upload completes; otherwise, <see langword="null" />.</returns>
    /// <exception cref="FileNotFoundException">Thrown when <paramref name="path" /> does not exist.</exception>
    /// <exception cref="NotSupportedException">Thrown when the media type, size, or upload method is unsupported.</exception>
    /// <exception cref="GooglePhotosException">Thrown when the upload protocol or API returns an error.</exception>
    public async Task<string?> UploadMediaAsync(string path, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart,
        Action<int>? callback = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"can't find '{path}'");
        var size = new FileInfo(path).Length;

        if (size < 1) throw new GooglePhotosException($"media file {path} has no data?");
        if (IsImage(Path.GetExtension(path)) && size > maxSizeImageBytes)
            throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeImageBytes} bytes!");
        if (IsVideo(Path.GetExtension(path)) && size > maxSizeVideoBytes)
            throw new NotSupportedException($"Media file {path} is too big for known upload limits of {maxSizeVideoBytes} bytes!");

        var headers = new List<(string name, string value)>
            {
                (X_Goog_Upload_Content_Type, GetMimeType())
            };
        if (uploadMethod == GooglePhotosUploadMethod.Simple)
            headers.Add((X_Goog_Upload_Protocol, "raw"));
        else if (new[] { GooglePhotosUploadMethod.ResumableSingle, GooglePhotosUploadMethod.ResumableMultipart }.Contains(uploadMethod))
        {
            headers.Add((X_Goog_Upload_Command, "start"));
            var fileName = Path.GetFileName(path);
            //Note: UrlPathEncode below is not intended to be used... but fixes https://github.com/f2calv/CasCap.Api.GooglePhotos/issues/110
            headers.Add((X_Goog_Upload_File_Name, HttpUtility.UrlPathEncode(fileName)));
            headers.Add((X_Goog_Upload_Protocol, "resumable"));
            headers.Add((X_Goog_Upload_Raw_Size, size.ToString()));
        }

        if (uploadMethod == GooglePhotosUploadMethod.Simple)
        {
            await using var stream = OpenReadStream(path);
            var tpl = await PostUploadStreamAsync(RequestUris.uploads, stream, headers, cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
            return tpl.result;
        }
        else
        {
            var tpl = await PostUploadBufferAsync(RequestUris.uploads, [], 0, headers, cancellationToken).ConfigureAwait(false);
            if (tpl.error is not null)
                throw new GooglePhotosException(tpl.error);

            var status = tpl.Header(X_Goog_Upload_Status);

            var Upload_URL = tpl.Header(X_Goog_Upload_URL) ?? throw new GooglePhotosException($"{nameof(X_Goog_Upload_URL)}");
            //Debug.WriteLine($"{Upload_URL}={Upload_URL}");
            var sUpload_Chunk_Granularity = tpl.Header(X_Goog_Upload_Chunk_Granularity);
            if (int.TryParse(sUpload_Chunk_Granularity, out var Upload_Chunk_Granularity) && Upload_Chunk_Granularity <= 0)
                throw new GooglePhotosException($"invalid {X_Goog_Upload_Chunk_Granularity}!");

            headers = [];

            if (uploadMethod == GooglePhotosUploadMethod.ResumableSingle)
            {
                headers.Add((X_Goog_Upload_Offset, "0"));
                headers.Add((X_Goog_Upload_Command, "upload, finalize"));

                await using var stream = OpenReadStream(path);
                tpl = await PostUploadStreamAsync(Upload_URL, stream, headers, cancellationToken).ConfigureAwait(false);
                if (tpl.httpStatusCode != HttpStatusCode.OK)
                {
                    //we were interrupted so query the status of the last upload
                    headers =
                        [
                            (X_Goog_Upload_Command, "query")
                        ];

                    tpl = await PostUploadBufferAsync(Upload_URL, [], 0, headers, cancellationToken).ConfigureAwait(false);
                    if (tpl.error is not null)
                        throw new GooglePhotosException(tpl.error);

                    status = tpl.Header(X_Goog_Upload_Status);
                    if (!string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(tpl.result))
                            return tpl.result;

                        throw new GooglePhotosException(
                            $"Resumable upload session terminated with status '{status ?? "missing"}' before an upload token was recovered.");
                    }

                    headers =
                    [
                        (X_Goog_Upload_Command, "upload, finalize"),
                        (X_Goog_Upload_Offset, "0")
                    ];
                    await using var retryStream = OpenReadStream(path);
                    tpl = await PostUploadStreamAsync(Upload_URL, retryStream, headers, cancellationToken).ConfigureAwait(false);
                    if (tpl.error is not null)
                        throw new GooglePhotosException(tpl.error);
                }

                return !string.IsNullOrWhiteSpace(tpl.result)
                    ? tpl.result
                    : throw new GooglePhotosException("Resumable upload completed without returning an upload token.");
            }
            else if (uploadMethod == GooglePhotosUploadMethod.ResumableMultipart)
            {
                var offset = 0L;
                var attemptCount = 0;
                var retryLimit = _options.Value.UploadRetryLimit;
                var batchIndex = 0;
                if (Upload_Chunk_Granularity <= 0)
                    throw new GooglePhotosException($"missing or invalid {X_Goog_Upload_Chunk_Granularity}!");

                var buffer = ArrayPool<byte>.Shared.Rent(Upload_Chunk_Granularity);
                try
                {
                    await using var stream = OpenReadStream(path);
                    while (offset < size)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        attemptCount++;
                        if (attemptCount > retryLimit)
                            throw new GooglePhotosException(
                                $"Resumable upload abandoned after {retryLimit} attempts at offset {offset} of {size} bytes.");

                        stream.Position = offset;
                        var bytesRead = await ReadChunkAsync(stream, buffer.AsMemory(0, Upload_Chunk_Granularity), cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                            throw new EndOfStreamException($"Unexpected end of media file at offset {offset}.");

                        var lastChunk = offset + bytesRead == size;
                        headers =
                            [
                                (X_Goog_Upload_Command, $"upload{(lastChunk ? ", finalize" : string.Empty)}"),
                                (X_Goog_Upload_Offset, offset.ToString())
                            ];

                        tpl = await PostUploadBufferAsync(Upload_URL, buffer, bytesRead, headers, cancellationToken).ConfigureAwait(false);
                        if (tpl.httpStatusCode != HttpStatusCode.OK)
                        {
                            headers =
                                [
                                    (X_Goog_Upload_Command, "query")
                                ];
                            tpl = await PostUploadBufferAsync(Upload_URL, [], 0, headers, cancellationToken).ConfigureAwait(false);
                            if (tpl.error is not null)
                                throw new GooglePhotosException(tpl.error);

                            status = tpl.Header(X_Goog_Upload_Status);
                            if (!string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrWhiteSpace(tpl.result))
                                    return tpl.result;

                                throw new GooglePhotosException(
                                    $"Resumable upload session terminated with status '{status ?? "missing"}' before an upload token was recovered.");
                            }

                            var sizeReceived = tpl.Header(X_Goog_Upload_Size_Received);
                            if (!long.TryParse(sizeReceived, out var receivedOffset)
                                || receivedOffset < 0
                                || receivedOffset > size)
                                throw new GooglePhotosException($"Missing or invalid {X_Goog_Upload_Size_Received}.");
                            if (receivedOffset == size)
                            {
                                stream.Position = 0;
                                headers =
                                [
                                    (X_Goog_Upload_Command, "upload, finalize"),
                                    (X_Goog_Upload_Offset, "0")
                                ];
                                tpl = await PostUploadStreamAsync(Upload_URL, stream, headers, cancellationToken).ConfigureAwait(false);
                                if (tpl.error is not null)
                                    throw new GooglePhotosException(tpl.error);

                                return !string.IsNullOrWhiteSpace(tpl.result)
                                    ? tpl.result
                                    : throw new GooglePhotosException("Resumable upload completed without returning an upload token.");
                            }

                            offset = receivedOffset;
                            LogUploadStatus(_logger, nameof(GooglePhotosServiceBase), nameof(UploadMediaAsync), status);
                        }
                        else
                        {
                            attemptCount = 0;
                            offset += bytesRead;
                            RaiseUploadProgressEvent(new UploadProgressEventArgs(Path.GetFileName(path), size, batchIndex, offset, bytesRead));
                            batchIndex++;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                return tpl.result;
            }
            else
                throw new NotSupportedException($"not supported upload type '{uploadMethod}'");
        }

        string GetMimeType()
        {
            var fileExtension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(fileExtension)) throw new NotSupportedException($"Missing file extension, unable to determine mime type for; {path}");
            if (IsImage(fileExtension))
                return MimeTypeMap.GetMimeType(fileExtension);
            else if (IsVideo(fileExtension))
                return MimeTypeMap.GetMimeType(fileExtension);
            else
                throw new NotSupportedException($"Cannot match file extension '{fileExtension}' from '{path}' to a known image or video mime type.");
        }
    }

    private Task<UploadResponse> PostUploadStreamAsync(
        string requestUri,
        Stream stream,
        List<(string name, string value)> headers,
        CancellationToken cancellationToken)
        => PostUploadContentAsync(requestUri, new StreamContent(stream), headers, cancellationToken);

    private Task<UploadResponse> PostUploadBufferAsync(
        string requestUri,
        byte[] buffer,
        int count,
        List<(string name, string value)> headers,
        CancellationToken cancellationToken)
        => PostUploadContentAsync(requestUri, new ByteArrayContent(buffer, 0, count), headers, cancellationToken);

    private async Task<UploadResponse> PostUploadContentAsync(
        string requestUri,
        HttpContent content,
        List<(string name, string value)> headers,
        CancellationToken cancellationToken)
    {
        var url = requestUri.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? requestUri : $"{Client.BaseAddress}{requestUri}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Headers.AddOrOverwrite(headers);

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
        //The response is disposed on the way out, so the protocol headers are copied rather than referenced.
        var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
            if (header.Key.StartsWith(UploadHeaders.Prefix, StringComparison.OrdinalIgnoreCase))
                responseHeaders[header.Key] = string.Join(',', header.Value);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return new UploadResponse(responseBody, null, response.StatusCode, responseHeaders);

        LogUploadFailed(_logger, nameof(GooglePhotosServiceBase), response.StatusCode);
        //TODO: a 401 on the upload endpoint is almost always a missing or insufficient OAuth scope rather than a
        //transport failure, and Google returns the opaque code 16 "Authentication session is not defined." Enrich
        //the returned Error with a scope hint so callers are not left guessing, and cover the externally supplied
        //token path from GooglePhotosCredentialProvider.SetAuthorization, which bypasses the granted-scope check.
        //See https://github.com/f2calv/CasCap.Api.GooglePhotos/issues/200
        if (responseBody.TryFromJson<Error>(out var error) && error?.ErrorStatus is not null)
            return new UploadResponse(null, error, response.StatusCode, responseHeaders);

        return new UploadResponse(null, new Error
        {
            ErrorStatus = new Status
            {
                Code = (int)response.StatusCode,
                Message = $"Upload failed with HTTP {(int)response.StatusCode}.",
                StatusName = response.StatusCode.ToString()
            }
        }, response.StatusCode, responseHeaders);
    }

    private readonly record struct UploadResponse(
        string? result,
        Error? error,
        HttpStatusCode httpStatusCode,
        IReadOnlyDictionary<string, string> responseHeaders)
    {
        internal string? Header(string name) => responseHeaders.TryGetValue(name, out var value) ? value : null;
    }

    private static FileStream OpenReadStream(string path)
        => new(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Share = FileShare.Read
        });

    private static async Task<int> ReadChunkAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;
        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer[totalBytesRead..], cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }

    private static AlbumPosition? GetAlbumPosition(string? albumId, GooglePhotosPositionType positionType, string? relativeMediaItemId, string? relativeEnrichmentItemId)
    {
        AlbumPosition? albumPosition = null;
        if (string.IsNullOrWhiteSpace(albumId)
            && (positionType != GooglePhotosPositionType.LastInAlbum || !string.IsNullOrWhiteSpace(relativeMediaItemId) || !string.IsNullOrWhiteSpace(relativeEnrichmentItemId)))
            throw new NotSupportedException($"cannot specify position without including an {nameof(albumId)}!");
        if (!string.IsNullOrWhiteSpace(relativeMediaItemId) && !string.IsNullOrWhiteSpace(relativeEnrichmentItemId))
            throw new NotSupportedException($"cannot specify {nameof(relativeMediaItemId)} and {nameof(relativeEnrichmentItemId)} at the same time!");
        if (positionType == GooglePhotosPositionType.LastInAlbum || positionType == GooglePhotosPositionType.Unspecified)
        {
            //the default so ignore
        }
        else if (positionType == GooglePhotosPositionType.FirstInAlbum)
            albumPosition = new AlbumPosition { Position = positionType };
        else if (!string.IsNullOrWhiteSpace(relativeMediaItemId))
            albumPosition = new AlbumPosition { Position = positionType, RelativeMediaItemId = relativeMediaItemId };
        else if (!string.IsNullOrWhiteSpace(relativeEnrichmentItemId))
            albumPosition = new AlbumPosition { Position = positionType, RelativeEnrichmentItemId = relativeEnrichmentItemId };
        else
            throw new NotSupportedException($"unexpected {nameof(positionType)} '{positionType}'?");
        return albumPosition;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{ClassName} {MethodName}, {Url}")]
    private static partial void LogRequestUrl(ILogger logger, string className, string methodName, string url);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{ClassName} {MethodName}, StatusCode={StatusCode}, StatusName={StatusName}")]
    private static partial void LogMediaItemStatus(ILogger logger, string className, string methodName, int statusCode, string? statusName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "{ClassName} {FilterName} element empty so removed from outgoing request")]
    private static partial void LogEmptyFilterRemoved(ILogger logger, string className, string filterName);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "{ClassName} {MethodName}, upload failed")]
    private static partial void LogMediaItemCreationFailed(ILogger logger, string className, string methodName);

    [LoggerMessage(EventId = 5, Level = LogLevel.Trace, Message = "{ClassName} {MethodName}, Status={Status}")]
    private static partial void LogUploadStatus(ILogger logger, string className, string methodName, string? status);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "{ClassName} upload failed with StatusCode={StatusCode}")]
    private static partial void LogUploadFailed(ILogger logger, string className, HttpStatusCode statusCode);
}
