namespace CasCap.Models;

/// <summary>Specifies a Google Photos feature used to filter media.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosFeatureType
{
    /// <summary>Media marked as a favorite.</summary>
    [JsonStringEnumMemberName("FAVORITES")]
    Favorites
}