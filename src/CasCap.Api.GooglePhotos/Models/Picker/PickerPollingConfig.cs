namespace CasCap.Models.Picker;

/// <summary>Provides Google's recommended interval and timeout for polling a Picker session.</summary>
public sealed record PickerPollingConfig
{
    /// <summary>Gets the recommended duration between session requests.</summary>
    [JsonPropertyName("pollInterval")]
    public string PollInterval { get; init; } = string.Empty;

    /// <summary>Gets the duration after which polling should stop.</summary>
    [JsonPropertyName("timeoutIn")]
    public string TimeoutIn { get; init; } = string.Empty;
}