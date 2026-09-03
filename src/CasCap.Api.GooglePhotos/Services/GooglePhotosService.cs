namespace CasCap.Services;

/// <summary>
/// This class chains together the inherited GooglePhotosServiceBase REST methods into more useful combos/actions.
/// </summary>
//https://developers.google.com/photos/library/guides/get-started
//https://developers.google.com/photos/library/guides/authentication-authorization
public sealed class GooglePhotosService(
    ILogger<GooglePhotosService> logger,
    IOptions<GooglePhotosOptions> options,
    GooglePhotosCredentialProvider credentialProvider,
    HttpClient client)
    : GooglePhotosServiceBase(logger, options, credentialProvider, client)
{
    /// <summary>Retrieves an album by title or creates it when no matching album exists.</summary>
    /// <param name="title">The album title to find or create.</param>
    /// <param name="comparisonType">The string comparison used to match album titles.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The matching or newly created album when available; otherwise, <see langword="null" />.</returns>
    public async Task<Album?> GetOrCreateAlbumAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase,
        CancellationToken cancellationToken = default)
    {
        var album = await GetAlbumByTitleAsync(title, comparisonType, cancellationToken: cancellationToken).ConfigureAwait(false);
        album ??= await CreateAlbumAsync(title, cancellationToken).ConfigureAwait(false);
        return album;
    }

    /// <summary>Finds the first application-created album with a matching title.</summary>
    /// <param name="title">The album title to find.</param>
    /// <param name="comparisonType">The string comparison used to match album titles.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The matching album when found; otherwise, <see langword="null" />.</returns>
    public async Task<Album?> GetAlbumByTitleAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase,
        CancellationToken cancellationToken = default)
    {
        var albums = await GetAlbumsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return albums.FirstOrDefault(p => p.Title.Equals(title, comparisonType));
    }

    /// <summary>Uploads one media file and creates the corresponding Google Photos media item.</summary>
    /// <param name="path">The local path of the media file.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="description">The optional media item description.</param>
    /// <param name="uploadMethod">The upload protocol to use.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The media item creation result when successful; otherwise, <see langword="null" />.</returns>
    public async Task<NewMediaItemResult?> UploadSingle(string path, string? albumId = null, string? description = null,
        GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, CancellationToken cancellationToken = default)
    {
        var uploadToken = await UploadMediaAsync(path, uploadMethod, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(uploadToken))
            return await AddMediaItemAsync(uploadToken!, path, description, albumId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return null;
    }

    /// <summary>Uploads multiple media files and creates their Google Photos media items.</summary>
    /// <param name="filePaths">The local paths of the media files.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="uploadMethod">The upload protocol to use.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The batch creation response when available; otherwise, <see langword="null" />.</returns>
    public Task<MediaItemsCreateResponse?> UploadMultiple(string[] filePaths, string? albumId = null,
        GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, CancellationToken cancellationToken = default)
        => _UploadMultiple(filePaths, albumId, uploadMethod, cancellationToken: cancellationToken);

    /// <summary>Uploads media files from a folder and creates their Google Photos media items.</summary>
    /// <param name="folderPath">The folder containing the media files.</param>
    /// <param name="searchPattern">An optional file search pattern.</param>
    /// <param name="albumId">The optional destination album identifier.</param>
    /// <param name="uploadMethod">The upload protocol to use.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The batch creation response when available; otherwise, <see langword="null" />.</returns>
    public Task<MediaItemsCreateResponse?> UploadMultiple(string folderPath, string? searchPattern = null, string? albumId = null,
        GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, CancellationToken cancellationToken = default)
    {
        var filePaths = searchPattern is not null ? Directory.GetFiles(folderPath, searchPattern) : Directory.GetFiles(folderPath);
        return _UploadMultiple(filePaths, albumId, uploadMethod, cancellationToken: cancellationToken);
    }

    private async Task<MediaItemsCreateResponse?> _UploadMultiple(string[] filePaths, string? albumId = null,
        GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart, CancellationToken cancellationToken = default)
    {
        var uploadItems = new List<UploadItem>(filePaths.Length);
        foreach (var filePath in filePaths)
        {
            var uploadToken = await UploadMediaAsync(filePath, uploadMethod, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(uploadToken))
                uploadItems.Add(new UploadItem(uploadToken!, filePath));
            //todo: raise photo uploaded event here
        }
        return await AddMediaItemsAsync(uploadItems, albumId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Download photo bytes. If the media item is a video then a thumbnail graphic of the video will be downloaded, use downloadVideoBytes get the raw bytes of the video.
    /// </summary>
    /// <param name="mediaItem">The media item whose bytes should be downloaded.</param>
    /// <param name="maxWidth">The optional maximum image width.</param>
    /// <param name="maxHeight">The optional maximum image height.</param>
    /// <param name="crop">Whether the image should be cropped to the requested dimensions.</param>
    /// <param name="includeExifMetadata">Whether downloadable photo bytes should include EXIF metadata.</param>
    /// <param name="downloadVideoBytes">Whether to download video bytes instead of a video thumbnail.</param>
    /// <param name="cancellationToken">A token that can cancel the download.</param>
    /// <returns>The downloaded bytes when available; otherwise, <see langword="null" />.</returns>
    /// <exception cref="GooglePhotosException">Thrown when the API returns an error.</exception>
    public Task<byte[]?> DownloadBytes(MediaItem mediaItem, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool includeExifMetadata = false, bool downloadVideoBytes = false, CancellationToken cancellationToken = default)
        => DownloadBytes(mediaItem.BaseUrl, maxWidth, maxHeight, crop, includeExifMetadata: mediaItem.IsPhoto && includeExifMetadata, downloadVideoBytes: mediaItem.IsVideo && downloadVideoBytes, cancellationToken: cancellationToken);

    //https://developers.google.com/photos/library/guides/access-media-items#image-base-urls
    //https://developers.google.com/photos/library/guides/access-media-items#video-base-urls
    private async Task<byte[]?> DownloadBytes(string baseUrl, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool includeExifMetadata = false, bool downloadVideoBytes = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentNullException(nameof(baseUrl), $"baseUrl is expected!");
        var qs = new List<string>();
        if (maxWidth.HasValue || maxHeight.HasValue)
        {
            if (maxWidth.HasValue) qs.Add($"w{maxWidth.Value}");
            if (maxHeight.HasValue) qs.Add($"h{maxHeight.Value}");
            if (crop) qs.Add("c");
        }
        if (includeExifMetadata) qs.Add("d");
        if (downloadVideoBytes) qs.Add("dv");
        if (qs.Count > 0)
            baseUrl += $"={string.Join("-", qs)}";
        var tpl = await Get<byte[], Error>(baseUrl, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (tpl.error is not null)
            throw new GooglePhotosException(tpl.error);
        else
            return tpl.result;
    }
}
