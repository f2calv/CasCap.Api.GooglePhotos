namespace CasCap.Tests;

/// <summary>Provides configuration, logging, and Google Photos services for integration tests.</summary>
/// <param name="output">The xUnit output helper used for test diagnostics.</param>
public abstract class TestBase(ITestOutputHelper output) : IDisposable
{
    private readonly (
        ConfigurationManager Configuration,
        ServiceProvider ServiceProvider,
        GooglePhotosPickerService PickerService,
        GooglePhotosService GooglePhotosService) _testServices = CreateTestServices(output);
    protected readonly ILogger _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();
    protected readonly ITestOutputHelper _output = output;
    protected readonly string _testFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata");
    private ConfigurationManager Configuration => _testServices.Configuration;
    private ServiceProvider ServiceProvider => _testServices.ServiceProvider;
    protected GooglePhotosPickerService GooglePhotosPickerSvc => _testServices.PickerService;
    protected GooglePhotosService GooglePhotosSvc => _testServices.GooglePhotosService;

    private static (
        ConfigurationManager Configuration,
        ServiceProvider ServiceProvider,
        GooglePhotosPickerService PickerService,
        GooglePhotosService GooglePhotosService) CreateTestServices(ITestOutputHelper output)
    {
        var configuration = new ConfigurationManager();
        configuration.AddStandardConfiguration("Test", typeof(TestBase).Assembly);

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
        Configuration.Dispose();
        GC.SuppressFinalize(this);
    }
}
