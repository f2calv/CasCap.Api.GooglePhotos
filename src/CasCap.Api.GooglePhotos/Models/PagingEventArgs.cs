namespace CasCap.Models;

/// <summary>Provides progress information while paging through API results.</summary>
public sealed class PagingEventArgs(int pageSize, int pageNumber, int recordCount) : EventArgs
{
    /// <summary>Gets the number of records returned in the page.</summary>
    public int PageSize { get; } = pageSize;

    /// <summary>Gets the one-based page number.</summary>
    public int PageNumber { get; } = pageNumber;

    /// <summary>Gets the cumulative record count.</summary>
    public int RecordCount { get; } = recordCount;

    /// <summary>Gets or sets the earliest creation date in the page.</summary>
    public DateTime? MinDate { get; set; }

    /// <summary>Gets or sets the latest creation date in the page.</summary>
    public DateTime? MaxDate { get; set; }
}
