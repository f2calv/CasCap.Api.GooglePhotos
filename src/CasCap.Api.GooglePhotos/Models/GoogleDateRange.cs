namespace CasCap.Models;

/// <summary>Represents an inclusive Google Photos date range.</summary>
public sealed class GoogleDateRange
{
    /// <summary>Gets or sets the start date.</summary>
    [JsonPropertyName("startDate")]
    public GoogleDate StartDate { get; set; } = default!;

    /// <summary>Gets or sets the end date.</summary>
    [JsonPropertyName("endDate")]
    public GoogleDate EndDate { get; set; } = default!;
}
