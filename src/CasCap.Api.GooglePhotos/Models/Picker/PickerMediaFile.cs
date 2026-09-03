namespace CasCap.Models.Picker;

/// <summary>Describes a media file selected through the Picker API.</summary>
public sealed record PickerMediaFile
{
    /// <summary>Gets the base URL used to retrieve the selected media bytes.</summary>
    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Gets the media MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; init; } = string.Empty;

    /// <summary>Gets the user-visible filename.</summary>
    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    /// <summary>Gets metadata describing the selected media file.</summary>
    [JsonPropertyName("mediaFileMetadata")]
    public PickerMediaFileMetadata MediaFileMetadata { get; init; } = new();
}
