namespace CasCap.Tests;

/// <summary>Creates a uniquely named media file under the temporary folder and deletes it on dispose.</summary>
internal sealed class TempMediaFile : IDisposable
{
    private TempMediaFile(string path) => Path = path;

    /// <summary>Gets the full path of the temporary file.</summary>
    internal string Path { get; }

    /// <summary>Writes the supplied bytes to a new temporary file with the given extension.</summary>
    /// <param name="bytes">The file contents.</param>
    /// <param name="cancellationToken">A token that can cancel the write.</param>
    /// <param name="extension">The file extension, including the leading period.</param>
    internal static async Task<TempMediaFile> CreateAsync(byte[] bytes, CancellationToken cancellationToken, string extension = ".jpg")
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return new TempMediaFile(path);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            //A leaked temp file must never fail an otherwise passing test.
        }
    }
}
