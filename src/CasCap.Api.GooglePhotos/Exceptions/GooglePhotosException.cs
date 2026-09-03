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
    public GooglePhotosException(Error error)
        : base(error is not null && error.ErrorStatus is not null && error.ErrorStatus.Message is not null ? error.ErrorStatus.Message : "unknown") { }
}
