namespace CasCap.Models;

/// <summary>Defines filters for a Google Photos media search.</summary>
public sealed class Filter
{
    /// <summary>Initializes an empty filter.</summary>
    public Filter() { }

    /// <summary>Initializes a filter for an inclusive date range.</summary>
    public Filter(DateTime startDate, DateTime endDate)
    {
        DateFilter = new DateFilter
        {
            Ranges = [new() { StartDate = new GoogleDate(startDate), EndDate = new GoogleDate(endDate) }]
        };
    }

    /// <summary>Initializes a filter for one content category.</summary>
    public Filter(GooglePhotosContentCategoryType category)
        => ContentFilter = new ContentFilter { IncludedContentCategories = [category] };

    /// <summary>Initializes a filter for content categories.</summary>
    public Filter(GooglePhotosContentCategoryType[] categories)
        => ContentFilter = new ContentFilter { IncludedContentCategories = categories };

    /// <summary>Initializes a filter for content categories.</summary>
    public Filter(List<GooglePhotosContentCategoryType> categories)
        => ContentFilter = new ContentFilter { IncludedContentCategories = categories.ToArray() };

    /// <summary>Gets or sets content-category criteria.</summary>
    [JsonPropertyName("contentFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentFilter? ContentFilter { get; set; }

    /// <summary>Gets or sets date criteria.</summary>
    [JsonPropertyName("dateFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateFilter? DateFilter { get; set; }

    /// <summary>Gets or sets feature criteria.</summary>
    [JsonPropertyName("featureFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FeatureFilter? FeatureFilter { get; set; }

    /// <summary>Gets or sets media-type criteria.</summary>
    [JsonPropertyName("mediaTypeFilter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MediaTypeFilter? MediaTypeFilter { get; set; }

    /// <summary>Gets or sets whether media not created by this application is excluded.</summary>
    [JsonPropertyName("excludeNonAppCreatedData")]
    public bool ExcludeNonAppCreatedData { get; set; }

    /// <summary>Gets or sets whether archived media is included.</summary>
    [JsonPropertyName("includeArchivedMedia")]
    public bool IncludeArchivedMedia { get; set; }
}
