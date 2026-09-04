namespace CasCap.Models.Picker;

/// <summary>Represents a photo or video explicitly selected by a user.</summary>
public sealed record PickedMediaItem
{
    /// <summary>Gets the persistent Picker media identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the media creation time.</summary>
    [JsonPropertyName("createTime")]
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>Gets the media type.</summary>
    [JsonPropertyName("type")]
    public PickedMediaItemType Type { get; init; }

    /// <summary>Gets the selected media file.</summary>
    [JsonPropertyName("mediaFile")]
    public PickerMediaFile MediaFile { get; init; } = new();
}
