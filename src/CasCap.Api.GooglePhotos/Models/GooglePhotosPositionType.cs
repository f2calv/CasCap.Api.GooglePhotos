namespace CasCap.Models;

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
