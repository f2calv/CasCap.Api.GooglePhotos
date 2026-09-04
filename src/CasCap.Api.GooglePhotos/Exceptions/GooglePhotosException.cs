namespace CasCap.Exceptions;

/// <summary>Represents an error returned by the Google Photos APIs or detected while processing a request.</summary>
public sealed class GooglePhotosException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="GooglePhotosException" /> class.</summary>
    public GooglePhotosException() { }

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosException" /> class with an error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public GooglePhotosException(string message) : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosException" /> class with an error message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public GooglePhotosException(string message, Exception? innerException) : base(message, innerException) { }

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosException" /> class from an API error response.</summary>
    /// <param name="error">The error returned by the Google Photos API.</param>
    public GooglePhotosException(Error error) : base(error?.ErrorStatus?.Message ?? "unknown")
        => Status = error?.ErrorStatus;

    /// <summary>Gets the status returned by the API, when the response carried one.</summary>
    /// <remarks>
    /// Use <see cref="Models.Status.StatusName" /> to branch on a canonical reason such as <c>RESOURCE_EXHAUSTED</c>
    /// or <c>PERMISSION_DENIED</c> rather than matching on <see cref="Exception.Message" />.
    /// </remarks>
    public Status? Status { get; }
}
