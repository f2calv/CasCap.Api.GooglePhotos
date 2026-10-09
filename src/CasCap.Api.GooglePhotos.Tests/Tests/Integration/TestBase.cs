namespace CasCap.Tests;

/// <summary>Provides configuration, logging, and Google Photos services for integration tests.</summary>
/// <param name="output">The xUnit output helper used for test diagnostics.</param>
public abstract class TestBase(ITestOutputHelper output) : IDisposable
{
    private readonly (
        IConfigurationRoot Configuration,
        ServiceProvider ServiceProvider,
        GooglePhotosPickerService PickerService,
        GooglePhotosService GooglePhotosService) _testServices = CreateTestServices(output);
    protected readonly ILogger _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();
    protected readonly ITestOutputHelper _output = output;
    protected readonly string _testFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata");
    private IConfigurationRoot Configuration => _testServices.Configuration;
    private ServiceProvider ServiceProvider => _testServices.ServiceProvider;
    protected GooglePhotosPickerService GooglePhotosPickerSvc => _testServices.PickerService;
    protected GooglePhotosService GooglePhotosSvc => _testServices.GooglePhotosService;

    private static (
        IConfigurationRoot Configuration,
        ServiceProvider ServiceProvider,
        GooglePhotosPickerService PickerService,
        GooglePhotosService GooglePhotosService) CreateTestServices(ITestOutputHelper output)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddUserSecrets<TestBase>()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        services.AddGooglePhotos(configuration);
        var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        return (
            configuration,
            serviceProvider,
            serviceProvider.GetRequiredService<GooglePhotosPickerService>(),
            serviceProvider.GetRequiredService<GooglePhotosService>());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ServiceProvider.Dispose();
        (Configuration as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
