namespace CasCap.Models;

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
