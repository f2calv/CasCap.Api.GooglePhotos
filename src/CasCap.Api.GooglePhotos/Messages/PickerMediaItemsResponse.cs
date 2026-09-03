using CasCap.Models.Picker;

namespace CasCap.Messages;

/// <summary>Represents a page of media selected through a Picker session.</summary>
internal sealed class PickerMediaItemsResponse
{
    /// <summary>Gets or sets the selected media items.</summary>
    [JsonPropertyName("mediaItems")]
    public List<PickedMediaItem> MediaItems { get; set; } = [];

    /// <summary>Gets or sets the token for the next page.</summary>
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
}