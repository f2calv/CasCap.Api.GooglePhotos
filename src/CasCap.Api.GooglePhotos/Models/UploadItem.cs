namespace CasCap.Models;

/// <summary>Defines an uploaded item to include in a media creation request.</summary>
public class UploadItem
{
    /// <summary>Initializes an uploaded item with an optional description.</summary>
    public UploadItem(string uploadToken, string? fileName, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadToken);
        UploadToken = uploadToken;
        FileName = Path.GetFileName(fileName);
        Description = description;
    }

    /// <summary>Initializes an uploaded item.</summary>
    public UploadItem(string uploadToken, string? fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadToken);
        UploadToken = uploadToken;
        FileName = Path.GetFileName(fileName);
    }

    /// <summary>Gets the upload token.</summary>
    public string UploadToken { get; }

    /// <summary>Gets the sanitized file name.</summary>
    public string? FileName { get; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; }
}