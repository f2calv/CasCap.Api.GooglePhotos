namespace CasCap.Models;

/// <summary>Provides relative request URI constants for the Google Photos Library API.</summary>
//TODO: request URIs are constants, not models. Move this and PickerRequestUris to a Constants folder once the
//namespace change can be taken, since CasCap.Models.RequestUris is public.
public static class RequestUris
{
    /// <summary>The base address of the Google Photos Library API.</summary>
    public const string BaseAddress = "https://photoslibrary.googleapis.com/v1/";

    /// <summary>
    /// POST Adds an enrichment at a specified position in a defined album.
    /// </summary>
    public const string POST_albums_addEnrichment = "albums/{0}:addEnrichment";

    /// <summary>
    /// POST Adds one or more media items in a user's Google Photos library to an album.
    /// </summary>
    public const string POST_albums_batchAddMediaItems = "albums/{0}:batchAddMediaItems";

    /// <summary>
    /// POST Removes one or more media items from a specified album.
    /// </summary>
    public const string POST_albums_batchRemoveMediaItems = "albums/{0}:batchRemoveMediaItems";

    /// <summary>
    /// GET Lists albums created by this application.
    /// </summary>
    public const string GET_albums = "albums";

    /// <summary>
    /// POST Creates an album in a user's Google Photos library.
    /// </summary>
    public const string POST_albums = "albums";

    /// <summary>
    /// Returns the album based on the specified albumId.
    /// </summary>
    public const string GET_album = "albums/{0}";

    /// <summary>The relative URI used to upload media bytes.</summary>
    public const string uploads = nameof(uploads);

    /// <summary>The relative URI used to list or retrieve media items.</summary>
    public const string GET_mediaItems = "mediaItems";

    /// <summary>The relative URI used to search for media items.</summary>
    public const string POST_mediaItems_search = "mediaItems:search";

    /// <summary>The relative URI used to create media items in a batch.</summary>
    public const string POST_mediaItems_batchCreate = "mediaItems:batchCreate";

    /// <summary>The relative URI used to retrieve media items in a batch.</summary>
    public const string GET_mediaItems_batchGet = "mediaItems:batchGet";
}
