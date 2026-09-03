namespace CasCap.Models;

/// <summary>Provides progress information while uploading media bytes.</summary>
public class UploadProgressEventArgs(string fileName, long totalBytes, int batchIndex, long uploadedBytes, long batchSize) : EventArgs
{
    /// <summary>Gets the uploaded filename.</summary>
    public string FileName { get; } = fileName;

    /// <summary>Gets the total media size in bytes.</summary>
    public long TotalBytes { get; } = totalBytes;

    /// <summary>Gets the zero-based upload batch index.</summary>
    public long BatchIndex { get; } = batchIndex;

    /// <summary>Gets the number of bytes uploaded so far.</summary>
    public long UploadedBytes { get; } = uploadedBytes;

    /// <summary>Gets the size of the current batch in bytes.</summary>
    public long BatchSize { get; } = batchSize;
}
