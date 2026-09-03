namespace CasCap.Models;

/// <summary>Represents a calendar date in the Google Photos API.</summary>
public sealed class GoogleDate
{
    /// <summary>Initializes an empty date.</summary>
    public GoogleDate() { }

    /// <summary>Initializes a date from a <see cref="DateTime" />.</summary>
    public GoogleDate(DateTime dateTime)
    {
        Year = dateTime.Year;
        Month = dateTime.Month;
        Day = dateTime.Day;
    }

    /// <summary>Initializes a date from its components.</summary>
    public GoogleDate(int year, int month, int day)
    {
        Year = year;
        Month = month;
        Day = day;
    }

    /// <summary>Gets or sets the year.</summary>
    [JsonPropertyName("year")]
    public int Year { get; set; }

    /// <summary>Gets or sets the month.</summary>
    [JsonPropertyName("month")]
    public int Month { get; set; }

    /// <summary>Gets or sets the day.</summary>
    [JsonPropertyName("day")]
    public int Day { get; set; }
}