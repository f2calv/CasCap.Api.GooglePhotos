namespace CasCap.Models;

public class UploadProgressEventArgs(string fileName, long totalBytes, int batchIndex, long uploadedBytes, long batchSize) : EventArgs
{
    public string fileName { get; } = fileName;
    public long totalBytes { get; } = totalBytes;
    public long batchIndex { get; } = batchIndex;
    public long uploadedBytes { get; } = uploadedBytes;
    public long batchSize { get; } = batchSize;
}
