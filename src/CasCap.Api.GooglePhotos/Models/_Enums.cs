namespace CasCap.Models;

/// <summary>Specifies a content category used to filter media.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosContentCategoryType
{
    /// <summary>Animals.</summary>
    [JsonStringEnumMemberName("ANIMALS")]
    Animals,

    /// <summary>Arts.</summary>
    [JsonStringEnumMemberName("ARTS")]
    Arts,

    /// <summary>Birthdays.</summary>
    [JsonStringEnumMemberName("BIRTHDAYS")]
    Birthdays,

    /// <summary>Cityscapes.</summary>
    [JsonStringEnumMemberName("CITYSCAPES")]
    Cityscapes,

    /// <summary>Crafts.</summary>
    [JsonStringEnumMemberName("CRAFTS")]
    Crafts,

    /// <summary>Documents.</summary>
    [JsonStringEnumMemberName("DOCUMENTS")]
    Documents,

    /// <summary>Fashion.</summary>
    [JsonStringEnumMemberName("FASHION")]
    Fashion,

    /// <summary>Flowers.</summary>
    [JsonStringEnumMemberName("FLOWERS")]
    Flowers,

    /// <summary>Food.</summary>
    [JsonStringEnumMemberName("FOOD")]
    Food,

    /// <summary>Gardens.</summary>
    [JsonStringEnumMemberName("GARDENS")]
    Gardens,

    /// <summary>Holidays.</summary>
    [JsonStringEnumMemberName("HOLIDAYS")]
    Holidays,

    /// <summary>Houses.</summary>
    [JsonStringEnumMemberName("HOUSES")]
    Houses,

    /// <summary>Landmarks.</summary>
    [JsonStringEnumMemberName("LANDMARKS")]
    Landmarks,

    /// <summary>Landscapes.</summary>
    [JsonStringEnumMemberName("LANDSCAPES")]
    Landscapes,

    /// <summary>Night scenes.</summary>
    [JsonStringEnumMemberName("NIGHT")]
    Night,

    /// <summary>People.</summary>
    [JsonStringEnumMemberName("PEOPLE")]
    People,

    /// <summary>Performances.</summary>
    [JsonStringEnumMemberName("PERFORMANCES")]
    Performances,

    /// <summary>Pets.</summary>
    [JsonStringEnumMemberName("PETS")]
    Pets,

    /// <summary>Receipts.</summary>
    [JsonStringEnumMemberName("RECEIPTS")]
    Receipts,

    /// <summary>Screenshots.</summary>
    [JsonStringEnumMemberName("SCREENSHOTS")]
    Screenshots,

    /// <summary>Selfies.</summary>
    [JsonStringEnumMemberName("SELFIES")]
    Selfies,

    /// <summary>Sport.</summary>
    [JsonStringEnumMemberName("SPORT")]
    Sport,

    /// <summary>Travel.</summary>
    [JsonStringEnumMemberName("TRAVEL")]
    Travel,

    /// <summary>Utility content.</summary>
    [JsonStringEnumMemberName("UTILITY")]
    Utility,

    /// <summary>Weddings.</summary>
    [JsonStringEnumMemberName("WEDDINGS")]
    Weddings,

    /// <summary>Whiteboards.</summary>
    [JsonStringEnumMemberName("WHITEBOARDS")]
    Whiteboards
}

/// <summary>Specifies a Google Photos feature used to filter media.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosFeatureType
{
    /// <summary>Media marked as a favorite.</summary>
    [JsonStringEnumMemberName("FAVORITES")]
    Favorites
}

/// <summary>Specifies a Google Photos media type.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosMediaType
{
    /// <summary>A photo.</summary>
    [JsonStringEnumMemberName("PHOTO")]
    Photo,

    /// <summary>A video.</summary>
    [JsonStringEnumMemberName("VIDEO")]
    Video
}

/// <summary>Specifies the position of a media or enrichment item within an album.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosPositionType
{
    /// <summary>Default value if this enum isn't set.</summary>
    [JsonStringEnumMemberName("POSITION_TYPE_UNSPECIFIED")]
    Unspecified,

    /// <summary>At the beginning of the album.</summary>
    [JsonStringEnumMemberName("FIRST_IN_ALBUM")]
    FirstInAlbum,

    /// <summary>At the end of the album.</summary>
    [JsonStringEnumMemberName("LAST_IN_ALBUM")]
    LastInAlbum,

    /// <summary>After a media item.</summary>
    [JsonStringEnumMemberName("AFTER_MEDIA_ITEM")]
    AfterMediaItem,

    /// <summary>After an enrichment item.</summary>
    [JsonStringEnumMemberName("AFTER_ENRICHMENT_ITEM")]
    AfterEnrichmentItem
}

/// <summary>Specifies an OAuth scope used by the Google Photos APIs.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosScope
{
    /// <summary>Creates media items, albums, and enrichments owned by the application.</summary>
    AppendOnly,

    /// <summary>Reads media items and albums created by the application.</summary>
    ReadOnlyAppCreatedData,

    /// <summary>Edits media items and albums created by the application.</summary>
    EditAppCreatedData,

    /// <summary>Creates Picker sessions and reads media items explicitly selected by the user.</summary>
    PickerMediaItemsReadOnly
}

/// <summary>Specifies the upload protocol used for media bytes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosUploadMethod
{
    /// <summary>Uploads all media bytes in one request.</summary>
    Simple,

    /// <summary>Uploads media bytes in one resumable request.</summary>
    ResumableSingle,

    /// <summary>Uploads media bytes in multiple resumable requests.</summary>
    ResumableMultipart
}
