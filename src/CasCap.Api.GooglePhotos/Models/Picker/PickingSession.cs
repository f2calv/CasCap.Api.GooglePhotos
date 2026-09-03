namespace CasCap.Models.Picker;

/// <summary>Represents a session in which a user selects Google Photos media.</summary>
public sealed record PickingSession
{
    /// <summary>Gets the Google-generated session identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the URI that the user opens to select media.</summary>
    [JsonPropertyName("pickerUri")]
    public string PickerUri { get; init; } = string.Empty;

    /// <summary>Gets Google's recommended polling configuration while selection is incomplete.</summary>
    [JsonPropertyName("pollingConfig")]
    public PickerPollingConfig? PollingConfig { get; init; }

    /// <summary>Gets the time at which access to the session expires.</summary>
    [JsonPropertyName("expireTime")]
    public DateTimeOffset ExpireTime { get; init; }

    /// <summary>Gets the configuration used to create the session.</summary>
    [JsonPropertyName("pickingConfig")]
    public PickerPickingConfig? PickingConfig { get; init; }

    /// <summary>Gets whether the user has completed media selection.</summary>
    [JsonPropertyName("mediaItemsSet")]
    public bool MediaItemsSet { get; init; }
}