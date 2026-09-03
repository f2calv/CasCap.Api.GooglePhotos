using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CasCap.Models;

public record GooglePhotosOptions
{
    /// <summary>
    /// Configuration sub-section locator key.
    /// </summary>
    public const string ConfigurationSectionName = $"{nameof(CasCap)}:{nameof(GooglePhotosOptions)}";

    [SetsRequiredMembers]
    public GooglePhotosOptions() { }

    /// <summary>
    /// The default endpoint for REST API requests, currently defaults to REST API v1.0
    /// </summary>
    [Required]
    public required string BaseAddress { get; set; } = RequestUris.BaseAddress;

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
    [Required]
    public required GooglePhotosScope[] Scopes { get; set; } = [GooglePhotosScope.Access, GooglePhotosScope.Sharing];

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

    /// <summary>
    /// e.g. Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth");
    /// </summary>
    public static string FileDataStoreFullPathDefault => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google.Apis.Auth");
}
