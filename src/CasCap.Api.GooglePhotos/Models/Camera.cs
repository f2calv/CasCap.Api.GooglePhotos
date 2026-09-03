namespace CasCap.Models;

/// <summary>Contains camera metadata shared by photos and videos.</summary>
public abstract class Camera
{
    /// <summary>Gets or sets the camera manufacturer.</summary>
    [JsonPropertyName("cameraMake")]
    public string? CameraMake { get; set; }

    /// <summary>Gets or sets the camera model.</summary>
    [JsonPropertyName("cameraModel")]
    public string? CameraModel { get; set; }
}
