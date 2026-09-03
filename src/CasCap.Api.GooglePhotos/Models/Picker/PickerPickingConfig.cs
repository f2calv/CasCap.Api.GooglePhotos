namespace CasCap.Models.Picker;

/// <summary>Configures the media selection experience for a Picker session.</summary>
public sealed record PickerPickingConfig
{
    /// <summary>Gets or initializes the maximum number of media items that can be selected.</summary>
    [JsonPropertyName("maxItemCount")]
    public string? MaxItemCount { get; init; }
}