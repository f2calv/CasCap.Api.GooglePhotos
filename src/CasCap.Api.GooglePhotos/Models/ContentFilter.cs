namespace CasCap.Models;

/// <summary>Defines included and excluded content categories.</summary>
public sealed class ContentFilter
{
    /// <summary>Gets or sets categories that must be included.</summary>
    [JsonPropertyName("includedContentCategories")]
    public GooglePhotosContentCategoryType[]? IncludedContentCategories { get; set; }

    /// <summary>Gets or sets categories that must be excluded.</summary>
    [JsonPropertyName("excludedContentCategories")]
    public GooglePhotosContentCategoryType[]? ExcludedContentCategories { get; set; }
}
