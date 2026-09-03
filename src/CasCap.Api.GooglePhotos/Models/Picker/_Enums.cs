namespace CasCap.Models.Picker;

/// <summary>Identifies the type of media selected through the Picker API.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PickedMediaItemType
{
    /// <summary>The media type is unspecified.</summary>
    [JsonStringEnumMemberName("TYPE_UNSPECIFIED")]
    Unspecified,

    /// <summary>The selected item is a photo.</summary>
    [JsonStringEnumMemberName("PHOTO")]
    Photo,

    /// <summary>The selected item is a video.</summary>
    [JsonStringEnumMemberName("VIDEO")]
    Video
}