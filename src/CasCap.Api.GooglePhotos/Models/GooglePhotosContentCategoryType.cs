namespace CasCap.Models;

/// <summary>Specifies a content category used to filter media.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GooglePhotosContentCategoryType
{
    /// <summary>Animals.</summary>
    [JsonStringEnumMemberName("ANIMALS")]
    Animals,

    /// <summary>Arts.</summary>
    [JsonStringEnumMemberName("ARTS")]
    Arts,

    /// <summary>Birthdays.</summary>
    [JsonStringEnumMemberName("BIRTHDAYS")]
    Birthdays,

    /// <summary>Cityscapes.</summary>
    [JsonStringEnumMemberName("CITYSCAPES")]
    Cityscapes,

    /// <summary>Crafts.</summary>
    [JsonStringEnumMemberName("CRAFTS")]
    Crafts,

    /// <summary>Documents.</summary>
    [JsonStringEnumMemberName("DOCUMENTS")]
    Documents,

    /// <summary>Fashion.</summary>
    [JsonStringEnumMemberName("FASHION")]
    Fashion,

    /// <summary>Flowers.</summary>
    [JsonStringEnumMemberName("FLOWERS")]
    Flowers,

    /// <summary>Food.</summary>
    [JsonStringEnumMemberName("FOOD")]
    Food,

    /// <summary>Gardens.</summary>
    [JsonStringEnumMemberName("GARDENS")]
    Gardens,

    /// <summary>Holidays.</summary>
    [JsonStringEnumMemberName("HOLIDAYS")]
    Holidays,

    /// <summary>Houses.</summary>
    [JsonStringEnumMemberName("HOUSES")]
    Houses,

    /// <summary>Landmarks.</summary>
    [JsonStringEnumMemberName("LANDMARKS")]
    Landmarks,

    /// <summary>Landscapes.</summary>
    [JsonStringEnumMemberName("LANDSCAPES")]
    Landscapes,

    /// <summary>Night scenes.</summary>
    [JsonStringEnumMemberName("NIGHT")]
    Night,

    /// <summary>People.</summary>
    [JsonStringEnumMemberName("PEOPLE")]
    People,

    /// <summary>Performances.</summary>
    [JsonStringEnumMemberName("PERFORMANCES")]
    Performances,

    /// <summary>Pets.</summary>
    [JsonStringEnumMemberName("PETS")]
    Pets,

    /// <summary>Receipts.</summary>
    [JsonStringEnumMemberName("RECEIPTS")]
    Receipts,

    /// <summary>Screenshots.</summary>
    [JsonStringEnumMemberName("SCREENSHOTS")]
    Screenshots,

    /// <summary>Selfies.</summary>
    [JsonStringEnumMemberName("SELFIES")]
    Selfies,

    /// <summary>Sport.</summary>
    [JsonStringEnumMemberName("SPORT")]
    Sport,

    /// <summary>Travel.</summary>
    [JsonStringEnumMemberName("TRAVEL")]
    Travel,

    /// <summary>Utility content.</summary>
    [JsonStringEnumMemberName("UTILITY")]
    Utility,

    /// <summary>Weddings.</summary>
    [JsonStringEnumMemberName("WEDDINGS")]
    Weddings,

    /// <summary>Whiteboards.</summary>
    [JsonStringEnumMemberName("WHITEBOARDS")]
    Whiteboards
}