using CasCap.Common.Services;
using Microsoft.AspNetCore.WebUtilities;
using MimeTypes;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;

namespace CasCap.Services;

public abstract class GooglePhotosServiceBase : HttpClientBase
{
    private const int maxSizeImageBytes = 1024 * 1024 * 200;
    private const long maxSizeVideoBytes = 1024 * 1024 * 1024 * 10L;

    private const int minPageSizeAlbums = 1;
    private const int defaultPageSizeAlbums = 50;
    private const int maxPageSizeAlbums = 50;

    private const int minPageSizeMediaItems = 1;
    private const int defaultPageSizeMediaItems = 100;
    private const int maxPageSizeMediaItems = 100;

    private const int defaultBatchSizeMediaItems = 50;

    private GooglePhotosOptions _options;

    protected GooglePhotosServiceBase(ILogger<GooglePhotosServiceBase> logger,
        IOptions<GooglePhotosOptions> options,
        HttpClient client
        )
    {
        _logger = logger;
        _options = options.Value;
        Client = client ?? throw new ArgumentNullException(nameof(client), $"{nameof(HttpClient)} cannot be null!");
    }

    protected virtual void RaisePagingEvent(PagingEventArgs args) => PagingEvent?.Invoke(this, args);
    public event EventHandler<PagingEventArgs>? PagingEvent;

    protected virtual void RaiseUploadProgressEvent(UploadProgressEventArgs args) => UploadProgressEvent?.Invoke(this, args);
    public event EventHandler<UploadProgressEventArgs>? UploadProgressEvent;

    public static bool IsFileUploadable(string path) => IsFileUploadableByExtension(Path.GetExtension(path));

    public static bool IsFileUploadableByExtension(string extension)
    {
        if (IsImage(extension))
            return true;
        if (IsVideo(extension))
            return true;
        return false;
    }

    private static bool IsImage(string extension) => AcceptedMimeTypesImage.Contains(MimeTypeMap.GetMimeType(extension));

    //https://developers.google.com/photos/library/guides/upload-media#file-types-sizes
    private static readonly HashSet<string> AcceptedMimeTypesImage = new(StringComparer.OrdinalIgnoreCase)
    {
        { "image/avif" },
        { "image/bmp" },
        { "image/gif" },
        { "image/heic" },
        { "image/vnd.microsoft.icon" },
        { "image/jpg" },
        { "image/jpeg" },
        { "image/png" },
        { "image/tiff" },
        { "image/webp" },
        { "image/x-panasonic-raw" },
        { "image/x-panasonic-rw2" },
    };

    private static bool IsVideo(string extension) => AcceptedMimeTypesVideo.Contains(MimeTypeMap.GetMimeType(extension));

    //todo: do we need to handle the mime types in a more forgiving way?
    private static readonly HashSet<string> AcceptedMimeTypesVideo = new(StringComparer.OrdinalIgnoreCase)
    {
        { "video/3gpp" },
        { "video/3gpp2" },
        { "video/x-ms-asf" },
        { "video/x-msvideo" },
        { "video/divx" },
        { "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
        { "video/mp4" },
        { "video/mp2t" },
        { "video/x-m4v" },
        { "video/x-matroska" },
        { "video/mmv" },//?
        { "video/mod" },//?
        { "video/quicktime" },
        { "video/mpeg" },//https://en.wikipedia.org/wiki/MPEG_transport_stream
        { "video/x-ms-wmv" },
    };

    public async Task<bool> LoginAsync(string User, string ClientId, string ClientSecret, GooglePhotosScope[] Scopes, string? FileDataStoreFullPathOverride = null, CancellationToken cancellationToken = default)
    {
        _options = new GooglePhotosOptions
        {
            User = User,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Scopes = Scopes,
            FileDataStoreFullPathOverride = FileDataStoreFullPathOverride
        };
        return await LoginAsync(cancellationToken);
    }

    public async Task<bool> LoginAsync(GooglePhotosOptions options, CancellationToken cancellationToken = default)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options), $"{nameof(GooglePhotosOptions)} cannot be null!");
        return await LoginAsync(cancellationToken);
    }

    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.User)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(_options.User)} cannot be null!");
        if (string.IsNullOrWhiteSpace(_options.ClientId)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(_options.ClientId)} cannot be null!");
        if (string.IsNullOrWhiteSpace(_options.ClientSecret)) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(_options.ClientSecret)} cannot be null!");
        if (_options.Scopes.IsNullOrEmpty()) throw new GooglePhotosException($"{nameof(GooglePhotosOptions)}.{nameof(_options.Scopes)} cannot be null/empty!");

        var authorization = await GooglePhotosAuthorization.AuthorizeAsync(_logger, _options, cancellationToken).ConfigureAwait(false);
        if (authorization is null)
            return false;

        Client.DefaultRequestHeaders.Authorization = authorization;
        return true;
    }

    /// <summary>
    /// Workaround to allow setting the auth header when running integration tests from CI.
    /// </summary>
    /// <param name="tokenType"></param>
    /// <param name="accessToken"></param>
    public void SetAuth(string tokenType, string accessToken)
        => Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);

    #region https://photoslibrary.googleapis.com/v1/albums

    //https://photoslibrary.googleapis.com/v1/albums/{albumId}
    public async Task<Album?> GetAlbumAsync(string albumId, CancellationToken cancellationToken = default)
    {
        var tpl = await Get<Album, Error>(string.Format(RequestUris.GET_album, albumId), cancellationToken: cancellationToken);

        return tpl.result;
    }

    /// <summary>Lists albums created by this application.</summary>
    public Task<List<Album>> GetAlbumsAsync(int pageSize = defaultPageSizeAlbums, CancellationToken cancellationToken = default)
        => _GetAlbumsAsync(RequestUris.GET_albums, pageSize, cancellationToken);

    //todo: add IPagable interface and merge with similar
    private async Task<List<Album>> _GetAlbumsAsync(string requestUri, int pageSize, CancellationToken cancellationToken)
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
            var tpl = await Get<AlbumsGetResponse, Error>(_requestUri, cancellationToken: cancellationToken);
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

    private string GetUrl(string uri, int? pageSize = defaultPageSizeAlbums, string? pageToken = null)
    {
        var queryParams = new Dictionary<string, string?>(2);
        if (pageSize.HasValue && pageSize != defaultPageSizeAlbums) queryParams.Add(nameof(pageSize), pageSize.Value.ToString());
        if (!string.IsNullOrWhiteSpace(pageToken)) queryParams.Add(nameof(pageToken), pageToken!);//todo: nullability look further into this
        var url = QueryHelpers.AddQueryString(uri, queryParams);
        _logger.LogDebug("{ClassName} {MethodName}, {Url}", nameof(GooglePhotosServiceBase), nameof(GetUrl), url);
        return url;
    }

    public async Task<Album?> CreateAlbumAsync(string title, CancellationToken cancellationToken = default)
    {
        var req = new { album = new Album { Title = title } };
        var tpl = await PostJson<Album, Error>(RequestUris.POST_albums, req, cancellationToken: cancellationToken);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result;
    }

    public Task<bool> AddMediaItemsToAlbumAsync(string albumId, string[] mediaItemIds, CancellationToken cancellationToken = default)
        => AddMediaItemsToAlbumAsync(albumId, mediaItemIds.ToList(), cancellationToken);

    public async Task<bool> AddMediaItemsToAlbumAsync(string albumId, List<string> mediaItemIds, CancellationToken cancellationToken = default)
    {
        var batches = mediaItemIds.Distinct().ToList().GetBatches(defaultBatchSizeMediaItems);
        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = new { mediaItemIds = batch.Value };
            var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchAddMediaItems, albumId), req, cancellationToken: cancellationToken);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        }
        return true;
    }

    public Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, string[] mediaItemIds, CancellationToken cancellationToken = default)
        => RemoveMediaItemsFromAlbumAsync(albumId, mediaItemIds.ToList(), cancellationToken);

    public async Task<bool> RemoveMediaItemsFromAlbumAsync(string albumId, List<string> mediaItemIds, CancellationToken cancellationToken = default)
    {
        var batches = mediaItemIds.GetBatches(defaultBatchSizeMediaItems);
        foreach (var batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var req = new { mediaItemIds = batch.Value };
            var tpl = await PostJson<string, Error>(string.Format(RequestUris.POST_albums_batchRemoveMediaItems, albumId), req, cancellationToken: cancellationToken);
            if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        }
        return true;
    }

    public async Task<EnrichmentItem?> AddEnrichmentToAlbumAsync(string albumId, NewEnrichmentItem newEnrichmentItem, AlbumPosition albumPosition, CancellationToken cancellationToken = default)
    {
        var tpl = await PostJson<AddEnrichmentResponse, Error>(string.Format(RequestUris.POST_albums_addEnrichment, albumId), new AddEnrichmentRequest(newEnrichmentItem, albumPosition), cancellationToken: cancellationToken);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result?.EnrichmentItem;
    }

    #endregion

    #region https://photoslibrary.googleapis.com/v1/mediaItems
    //todo: find a neater way to merge _GetMediaItemsAsync & _GetMediaItemsViaPOSTAsync - practically the same - pass an Action?
    //todo: add IPagable interface and merge with similar
    private async IAsyncEnumerable<MediaItem> _GetMediaItemsAsync(int pageSize, int maxPageCount, string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            var tpl = await Get<MediaItemsResponse, Error>(_requestUri, cancellationToken: cancellationToken);
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

    //todo: add IPagable interface and merge with similar
    private async IAsyncEnumerable<MediaItem> _GetMediaItemsViaPOSTAsync(string? albumId, int pageSize, int maxPageCount, Filter? filters, string requestUri, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            var tpl = await PostJson<MediaItemsResponse, Error>(requestUri, req, cancellationToken: cancellationToken);
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

    public IAsyncEnumerable<MediaItem> GetMediaItemsAsync(int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => _GetMediaItemsAsync(pageSize, maxPageCount, RequestUris.GET_mediaItems, cancellationToken);

    public IAsyncEnumerable<MediaItem> GetMediaItemsByAlbumAsync(string albumId, int pageSize = defaultPageSizeMediaItems, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => _GetMediaItemsViaPOSTAsync(albumId, pageSize, maxPageCount, null, RequestUris.POST_mediaItems_search, cancellationToken);

    //https://photoslibrary.googleapis.com/v1/mediaItems/media-item-id
    public async Task<MediaItem?> GetMediaItemByIdAsync(string mediaItemId, CancellationToken cancellationToken = default)
    {
        var tpl = await Get<MediaItem, Error>($"{RequestUris.GET_mediaItems}/{mediaItemId}", cancellationToken: cancellationToken);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result;
    }

    //https://photoslibrary.googleapis.com/v1/mediaItems:batchGet?mediaItemIds=media-item-id&mediaItemIds=another-media-item-id&mediaItemIds=incorrect-media-item-id
    public IAsyncEnumerable<MediaItem> GetMediaItemsByIdsAsync(string[] mediaItemIds, CancellationToken cancellationToken = default)
        => GetMediaItemsByIdsAsync(mediaItemIds.ToList(), cancellationToken);

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
            var tpl = await Get<MediaItemsGetResponse, Error>(url, cancellationToken: cancellationToken);
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
                        _logger.LogWarning("{ClassName} {MethodName}, status={Status}", nameof(GooglePhotosServiceBase),
                            nameof(GetMediaItemsByIdsAsync), result.Status);//we highlight if any objects returned a non-null status object
                }
                if (batch.Key + 1 != batches.Count)
                    RaisePagingEvent(new PagingEventArgs(tpl.result.MediaItemResults.Count, batch.Key + 1, hs.Count));
            }
        }
    }

    public IAsyncEnumerable<MediaItem> GetMediaItemsByDateRangeAsync(DateTime startDate, DateTime endDate, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(startDate, endDate), maxPageCount, cancellationToken);

    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoryAsync(GooglePhotosContentCategoryType category, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(category), maxPageCount, cancellationToken);

    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoriesAsync(GooglePhotosContentCategoryType[] categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

    public IAsyncEnumerable<MediaItem> GetMediaItemsByCategoriesAsync(List<GooglePhotosContentCategoryType> categories, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => GetMediaItemsByFilterAsync(new Filter(categories), maxPageCount, cancellationToken);

    public IAsyncEnumerable<MediaItem> GetMediaItemsByFilterAsync(Filter filter, int maxPageCount = int.MaxValue, CancellationToken cancellationToken = default)
        => _GetMediaItemsByFilterAsync(filter, maxPageCount, cancellationToken);

    private IAsyncEnumerable<MediaItem> _GetMediaItemsByFilterAsync(Filter filter, int maxPageCount, CancellationToken cancellationToken)
    {
        //validate/tidy outgoing filter object
        var contentFilter = filter.ContentFilter;
        if (contentFilter is not null)
        {
            if (contentFilter.IncludedContentCategories.IsNullOrEmpty()) contentFilter.IncludedContentCategories = null;
            if (contentFilter.ExcludedContentCategories.IsNullOrEmpty()) contentFilter.ExcludedContentCategories = null;
            if (contentFilter.IncludedContentCategories is null && contentFilter.ExcludedContentCategories is null)
                _logger.LogDebug($"{nameof(contentFilter)} element empty so removed from outgoing request");
        }
        var dateFilter = filter.DateFilter;
        if (dateFilter is not null)
        {
            if (dateFilter.Dates.IsNullOrEmpty()) dateFilter.Dates = null;
            if (dateFilter.Ranges.IsNullOrEmpty()) dateFilter.Ranges = null;
            if (dateFilter.Dates is null && dateFilter.Ranges is null)
                _logger.LogDebug($"{nameof(dateFilter)} element empty so removed from outgoing request");
            //do we need to validate start/end date ranges, i.e. start before end...?
        }
        var mediaTypeFilter = filter.MediaTypeFilter;
        if (mediaTypeFilter is not null && mediaTypeFilter.MediaTypes.IsNullOrEmpty())
            _logger.LogDebug($"{nameof(mediaTypeFilter)} element empty so removed from outgoing request");

        var featureFilter = filter.FeatureFilter;
        if (featureFilter is not null && featureFilter.IncludedFeatures.IsNullOrEmpty())
            _logger.LogDebug($"{nameof(featureFilter)} element empty so removed from outgoing request");

        return _GetMediaItemsViaPOSTAsync(null, defaultPageSizeMediaItems, maxPageCount, filter, RequestUris.POST_mediaItems_search, cancellationToken);
    }

    //would need renaming if made public
    private Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null, AlbumPosition? albumPosition = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, albumPosition, cancellationToken);

    public Task<NewMediaItemResult?> AddMediaItemAsync(string uploadToken, string? fileName = null, string? description = null, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(new UploadItem(uploadToken, fileName, description), albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    public Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemAsync(uploadItem, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    //would need renaming if made public
    private async Task<NewMediaItemResult?> AddMediaItemAsync(UploadItem uploadItem, string? albumId, AlbumPosition? albumPosition, CancellationToken cancellationToken)
    {
        var newMediaItems = new List<UploadItem> { uploadItem };
        var res = await AddMediaItemsAsync(newMediaItems, albumId, albumPosition, cancellationToken);
        if (res is not null && !res.NewMediaItemResults.IsNullOrEmpty())
            return res.NewMediaItemResults[0];
        else
        {
            _logger.LogError("{ClassName} {MethodName}, upload failure '{FileName}'", nameof(GooglePhotosServiceBase),
                nameof(AddMediaItemAsync), uploadItem.FileName);
            return null;
        }
    }

    public Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<(string uploadToken, string FileName)> items, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
    {
        var uploadItems = new List<UploadItem>(items.Count);
        foreach (var item in items)
            uploadItems.Add(new UploadItem(item.uploadToken, item.FileName));
        return AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);
    }

    public Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId = null,
        GooglePhotosPositionType positionType = GooglePhotosPositionType.LastInAlbum, string? relativeMediaItemId = null, string? relativeEnrichmentItemId = null, CancellationToken cancellationToken = default)
        => AddMediaItemsAsync(uploadItems, albumId, GetAlbumPosition(albumId, positionType, relativeMediaItemId, relativeEnrichmentItemId), cancellationToken);

    //would need renaming if made public
    private async Task<MediaItemsCreateResponse?> AddMediaItemsAsync(List<UploadItem> uploadItems, string? albumId, AlbumPosition? albumPosition, CancellationToken cancellationToken)
    {
        if (uploadItems.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(uploadItems), $"Invalid {nameof(uploadItems)} quantity, must be >= 1");
        var newMediaItems = new List<NewMediaItem>(uploadItems.Count);
        foreach (var mediaItem in uploadItems)
        {
            var newMediaItem = new NewMediaItem
            {
                Description = mediaItem.Description,
                SimpleMediaItem = new SimpleMediaItem
                {
                    FileName = mediaItem.FileName,
                    UploadToken = mediaItem.UploadToken,
                }
            };
            newMediaItems.Add(newMediaItem);
        }
        var req = new { newMediaItems, albumId, albumPosition };
        var tpl = await PostJson<MediaItemsCreateResponse, Error>(RequestUris.POST_mediaItems_batchCreate, req, cancellationToken: cancellationToken);
        if (tpl.error is not null) throw new GooglePhotosException(tpl.error);
        return tpl.result;
    }
    #endregion

    private const string X_Goog_Upload_Content_Type = "X-Goog-Upload-Content-Type";
    private const string X_Goog_Upload_Protocol = "X-Goog-Upload-Protocol";
    private const string X_Goog_Upload_Command = "X-Goog-Upload-Command";
    private const string X_Goog_Upload_File_Name = "X-Goog-Upload-File-Name";
    private const string X_Goog_Upload_Raw_Size = "X-Goog-Upload-Raw-Size";
    private const string X_Goog_Upload_URL = "X-Goog-Upload-URL";
    private const string X_Goog_Upload_Offset = "X-Goog-Upload-Offset";
    private const string X_Goog_Upload_Status = "X-Goog-Upload-Status";
    private const string X_Goog_Upload_Chunk_Granularity = "X-Goog-Upload-Chunk-Granularity";
    private const string X_Goog_Upload_Size_Received = "X-Goog-Upload-Size-Received";

    //todo: refactor this method when time, it's a bit of a mess :/
    //https://developers.google.com/photos/library/guides/upload-media
    //https://developers.google.com/photos/library/guides/upload-media#uploading-bytes
    //https://developers.google.com/photos/library/guides/resumable-uploads
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
            var tpl = await PostBytes<string, Error>(RequestUris.uploads, [], additionalHeaders: headers, cancellationToken: cancellationToken);
            var status = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);

            var Upload_URL = tpl.responseHeaders.TryGetValue(X_Goog_Upload_URL) ?? throw new GooglePhotosException($"{nameof(X_Goog_Upload_URL)}");
            //Debug.WriteLine($"{Upload_URL}={Upload_URL}");
            var sUpload_Chunk_Granularity = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Chunk_Granularity);
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

                    tpl = await PostBytes<string, Error>(Upload_URL, [], additionalHeaders: headers, cancellationToken: cancellationToken);
                    if (tpl.error is not null) throw new GooglePhotosException(tpl.error);

                    _ = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);
                    _ = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Size_Received);
                }

                return tpl.result;
            }
            else if (uploadMethod == GooglePhotosUploadMethod.ResumableMultipart)
            {
                var offset = 0L;
                var attemptCount = 0;
                var retryLimit = 10;//todo: move this into settings
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
                            return null;

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
                            tpl = await PostBytes<string, Error>(Upload_URL, [], additionalHeaders: headers, cancellationToken: cancellationToken);

                            status = tpl.responseHeaders.TryGetValue(X_Goog_Upload_Status);
                            _logger.LogTrace("{ClassName} {MethodName}, Status={Status}", nameof(GooglePhotosServiceBase),
                                nameof(UploadMediaAsync), status);
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

    private Task<(string? result, Error? error, HttpStatusCode httpStatusCode, HttpResponseHeaders responseHeaders)> PostUploadStreamAsync(
        string requestUri,
        Stream stream,
        List<(string name, string value)> headers,
        CancellationToken cancellationToken)
        => PostUploadContentAsync(requestUri, new StreamContent(stream), headers, cancellationToken);

    private Task<(string? result, Error? error, HttpStatusCode httpStatusCode, HttpResponseHeaders responseHeaders)> PostUploadBufferAsync(
        string requestUri,
        byte[] buffer,
        int count,
        List<(string name, string value)> headers,
        CancellationToken cancellationToken)
        => PostUploadContentAsync(requestUri, new ByteArrayContent(buffer, 0, count), headers, cancellationToken);

    private async Task<(string? result, Error? error, HttpStatusCode httpStatusCode, HttpResponseHeaders responseHeaders)> PostUploadContentAsync(
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
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            return (responseBody, null, response.StatusCode, response.Headers);

        _logger.LogError("{ClassName} upload failed with StatusCode={StatusCode}", nameof(GooglePhotosServiceBase), response.StatusCode);
        return (null, responseBody.FromJson<Error>(), response.StatusCode, response.Headers);
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
}
