namespace CasCap.Models;

public class PagingEventArgs(int pageSize, int pageNumber, int recordCount) : EventArgs
{
    public int pageSize { get; } = pageSize;
    public int pageNumber { get; } = pageNumber;
    public int recordCount { get; } = recordCount;
    public DateTime? minDate { get; set; }
    public DateTime? maxDate { get; set; }
}
