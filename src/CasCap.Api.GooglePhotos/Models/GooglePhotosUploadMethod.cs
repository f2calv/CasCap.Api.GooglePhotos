namespace CasCap.Models;

/// <summary>Specifies the upload protocol used for media bytes.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosUploadMethod
{
    /// <summary>Uploads all media bytes in one request.</summary>
    Simple,

    /// <summary>Uploads media bytes in one resumable request.</summary>
    ResumableSingle,

    /// <summary>Uploads media bytes in multiple resumable requests.</summary>
    ResumableMultipart
}
