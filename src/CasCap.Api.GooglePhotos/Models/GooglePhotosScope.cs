namespace CasCap.Models;

/// <summary>Specifies an OAuth scope used by the Google Photos APIs.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosScope
{
    /// <summary>Creates media items, albums, and enrichments owned by the application.</summary>
    AppendOnly,

    /// <summary>Reads media items and albums created by the application.</summary>
    ReadOnlyAppCreatedData,

    /// <summary>Edits media items and albums created by the application.</summary>
    EditAppCreatedData,

    /// <summary>Creates Picker sessions and reads media items explicitly selected by the user.</summary>
    PickerMediaItemsReadOnly
}