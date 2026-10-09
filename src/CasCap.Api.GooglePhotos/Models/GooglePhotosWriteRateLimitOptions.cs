using System.ComponentModel.DataAnnotations;

namespace CasCap.Models;

/// <summary>Configures client-side temporal rate limiting for mutating Google Photos Library API requests.</summary>
public sealed record GooglePhotosWriteRateLimitOptions
{
    /// <summary>Gets or sets whether client-side write rate limiting is active.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the maximum number of write requests permitted during each window.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 8;

    /// <summary>Gets or sets the maximum number of write requests waiting for permits.</summary>
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 100;

    /// <summary>Gets or sets the number of segments used to smooth requests across each window.</summary>
    [Range(1, int.MaxValue)]
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>Gets or sets the rate-limit window duration in seconds.</summary>
    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}