using CasCap.Models.Picker;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CasCap.Models;

/// <summary>Configures Google Photos API endpoints, OAuth credentials, scopes, and client-side request behavior.</summary>
public sealed record GooglePhotosOptions
{
    /// <summary>
    /// Configuration sub-section locator key.
    /// </summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(GooglePhotosOptions)}";

    /// <summary>Initializes a new instance of the <see cref="GooglePhotosOptions" /> class with default endpoints and scopes.</summary>
    [SetsRequiredMembers]
    public GooglePhotosOptions() { }

    /// <summary>
    /// The default endpoint for REST API requests, currently defaults to REST API v1.0
    /// </summary>
    [Required, Url]
    public required string BaseAddress { get; set; } = RequestUris.BaseAddress;

    /// <summary>Gets or sets the endpoint for Google Photos Picker API requests.</summary>
    [Required, Url]
    public required string PickerBaseAddress { get; set; } = PickerRequestUris.BaseAddress;

    /// <summary>
    /// The email address of the Google Account that holds the photos.
    /// e.g. your.email@mydomain.com
    /// </summary>
    [Required]
    public required string User { get; set; } = string.Empty;

    /// <summary>
    /// Security Scopes, i.e. access levels.
    /// Note: When changing scopes under the same User you must manually delete the local JSON file to clear the local cache,
    /// you can use the GooglePhotosOptions.FileDataStoreFullPathDefault property to locate the path to the JSON file(s).
    /// </summary>
    [Required, MinLength(1)]
    public required GooglePhotosScope[] Scopes { get; set; } =
    [
        GooglePhotosScope.AppendOnly,
        GooglePhotosScope.ReadOnlyAppCreatedData,
        GooglePhotosScope.EditAppCreatedData,
        GooglePhotosScope.PickerMediaItemsReadOnly
    ];

    /// <summary>
    /// Google Client Id string, numerical/alphanumeric.
    /// e.g. 012345678901-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.apps.googleusercontent.com
    /// </summary>
    [Required]
    public required string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Google Client Secret string, alphabetical.
    /// i.e. abcabcabcabcabcabcabcabc
    /// </summary>
    [Required]
    public required string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Folder path to locally cache OAuth 2.0 JSON file.
    /// If FileDataStoreFullPathOverride is left as null then Google Auth library will use the
    /// FileDataStoreFullPathDefault location to store a cached OAuth 2.0 JSON file per User.
    /// </summary>
    public string? FileDataStoreFullPathOverride { get; set; }

    /// <summary>Gets or sets the optional client-side limiter for mutating Library API requests.</summary>
    [Required, ValidateObjectMembers]
    public GooglePhotosWriteRateLimitOptions WriteRateLimit { get; set; } = new();

    /// <summary>
    /// e.g. Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth");
    /// </summary>
    public static string FileDataStoreFullPathDefault => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth");
}

/// <summary>Configures client-side temporal rate limiting for mutating Google Photos Library API requests.</summary>
public sealed record GooglePhotosWriteRateLimitOptions
{
    /// <summary>Gets or sets whether client-side write rate limiting is active.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the maximum number of write requests permitted during each window.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 8;

    /// <summary>Gets or sets the maximum number of write requests waiting for permits.</summary>
    [Range(0, int.MaxValue)]
    public int QueueLimit { get; set; } = 100;

    /// <summary>Gets or sets the number of segments used to smooth requests across each window.</summary>
    [Range(1, int.MaxValue)]
    public int SegmentsPerWindow { get; set; } = 6;

    /// <summary>Gets or sets the rate-limit window duration in seconds.</summary>
    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}
