namespace CasCap.Models;

/// <summary>Defines exact dates and date ranges for a media search.</summary>
public sealed class DateFilter
{
    /// <summary>Gets or sets exact dates to include.</summary>
    [JsonPropertyName("dates")]
    public GoogleDate[]? Dates { get; set; }

    /// <summary>Gets or sets date ranges to include.</summary>
    [JsonPropertyName("ranges")]
    public GoogleDateRange[]? Ranges { get; set; }
}
