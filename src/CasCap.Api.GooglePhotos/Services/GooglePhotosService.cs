﻿namespace CasCap.Services;

/// <summary>
/// This class chains together the inherited GooglePhotosServiceBase REST methods into more useful combos/actions.
/// </summary>
//https://developers.google.com/photos/library/guides/get-started
//https://developers.google.com/photos/library/guides/authentication-authorization
public class GooglePhotosService(ILogger<GooglePhotosService> logger, IOptions<GooglePhotosOptions> options, HttpClient client)
    : GooglePhotosServiceBase(logger, options, client)
{
    public async Task<Album?> GetOrCreateAlbumAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        var album = await GetAlbumByTitleAsync(title, comparisonType);
        album ??= await CreateAlbumAsync(title);
        return album;
    }

    public async Task<Album?> GetAlbumByTitleAsync(string title, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        var albums = await GetAlbumsAsync();
        return albums.FirstOrDefault(p => p.title.Equals(title, comparisonType));
    }

    public async Task<NewMediaItemResult?> UploadSingle(string path, string? albumId = null, string? description = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
    {
        var uploadToken = await UploadMediaAsync(path, uploadMethod);
        if (!string.IsNullOrWhiteSpace(uploadToken))
            return await AddMediaItemAsync(uploadToken!, path, description, albumId);
        return null;
    }

    public Task<mediaItemsCreateResponse?> UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
        => _UploadMultiple(filePaths, albumId, uploadMethod);

    public Task<mediaItemsCreateResponse?> UploadMultiple(string folderPath, string? searchPattern = null, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
    {
        var filePaths = searchPattern is not null ? Directory.GetFiles(folderPath, searchPattern) : Directory.GetFiles(folderPath);
        return _UploadMultiple(filePaths, albumId, uploadMethod);
    }

    async Task<mediaItemsCreateResponse?> _UploadMultiple(string[] filePaths, string? albumId = null, GooglePhotosUploadMethod uploadMethod = GooglePhotosUploadMethod.ResumableMultipart)
    {
        var uploadItems = new List<UploadItem>(filePaths.Length);
        foreach (var filePath in filePaths)
        {
            var uploadToken = await UploadMediaAsync(filePath, uploadMethod);
            if (!string.IsNullOrWhiteSpace(uploadToken))
                uploadItems.Add(new UploadItem(uploadToken!, filePath));
            //todo: raise photo uploaded event here
        }
        return await AddMediaItemsAsync(uploadItems, albumId);
    }

    /// <summary>
    /// Download photo bytes. If the media item is a video then a thumbnail graphic of the video will be downloaded, use downloadVideoBytes get the raw bytes of the video.
    /// </summary>
    public Task<byte[]?> DownloadBytes(MediaItem mediaItem, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool includeExifMetadata = false, bool downloadVideoBytes = false)
        => DownloadBytes(mediaItem.baseUrl, maxWidth, maxHeight, crop, includeExifMetadata: mediaItem.isPhoto && includeExifMetadata, downloadVideoBytes: mediaItem.isVideo && downloadVideoBytes);

    //https://developers.google.com/photos/library/guides/access-media-items#image-base-urls
    //https://developers.google.com/photos/library/guides/access-media-items#video-base-urls
    async Task<byte[]?> DownloadBytes(string baseUrl, int? maxWidth = null, int? maxHeight = null, bool crop = false, bool includeExifMetadata = false, bool downloadVideoBytes = false)
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
        var tpl = await Get<byte[], Error>(baseUrl);
        if (tpl.error is not null)
            throw new GooglePhotosException(tpl.error);
        else
            return tpl.result;
    }
}
