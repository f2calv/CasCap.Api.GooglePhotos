using System.Collections;
using System.Reflection;

namespace CasCap.Tests;

/// <summary>Tests Google Photos dependency-injection registration and resilience configuration.</summary>
[Trait("Category", "Registration")]
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RegistrationResolvesTypedClients()
    {
        var services = new ServiceCollection();
        services.AddGooglePhotos(CreateValidOptions());
        using var serviceProvider = services.BuildServiceProvider();

        //Resolving a typed client builds its handler chain, which validates the standard resilience options.
        var libraryService = serviceProvider.GetRequiredService<GooglePhotosService>();
        var pickerService = serviceProvider.GetRequiredService<GooglePhotosPickerService>();

        Assert.Equal(new Uri(RequestUris.BaseAddress), libraryService.Client.BaseAddress);
        Assert.Equal(new Uri(PickerRequestUris.BaseAddress), pickerService.Client.BaseAddress);
    }

    [Fact]
    public void RegistrationCopiesEveryOption()
    {
        var supplied = new GooglePhotosOptions
        {
            BaseAddress = "https://library.example.test/v1/",
            PickerBaseAddress = "https://picker.example.test/v1/",
            User = "local-user",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = [GooglePhotosScope.EditAppCreatedData],
            FileDataStoreFullPathOverride = Path.Combine(Path.GetTempPath(), "token-cache"),
            RequestTimeoutSeconds = 11,
            UploadTimeoutSeconds = 22,
            WriteRateLimit = new GooglePhotosWriteRateLimitOptions
            {
                Enabled = true,
                PermitLimit = 3,
                QueueLimit = 4,
                SegmentsPerWindow = 5,
                WindowSeconds = 6
            }
        };
        var services = new ServiceCollection();
        services.AddGooglePhotos(supplied);
        using var serviceProvider = services.BuildServiceProvider();

        var resolved = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;

        var defaults = new GooglePhotosOptions();
        foreach (var property in typeof(GooglePhotosOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.SetMethod is null)
                continue;

            var expected = property.GetValue(supplied);
            Assert.NotEqual(property.GetValue(defaults), expected);
            var actual = property.GetValue(resolved);
            if (expected is IEnumerable expectedItems and not string)
                Assert.Equal(expectedItems.Cast<object>(), ((IEnumerable)actual!).Cast<object>());
            else
                Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RegistrationRejectsInvalidRequestTimeout(int requestTimeoutSeconds)
    {
        var options = CreateValidOptions();
        options.RequestTimeoutSeconds = requestTimeoutSeconds;
        var services = new ServiceCollection();
        services.AddGooglePhotos(options);
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RegistrationRejectsInvalidUploadTimeout(int uploadTimeoutSeconds)
    {
        var options = CreateValidOptions();
        options.UploadTimeoutSeconds = uploadTimeoutSeconds;
        var services = new ServiceCollection();
        services.AddGooglePhotos(options);
        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value);
    }

    private static GooglePhotosOptions CreateValidOptions()
        => new()
        {
            User = "local-user",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scopes = [GooglePhotosScope.AppendOnly, GooglePhotosScope.PickerMediaItemsReadOnly]
        };
}
