namespace CasCap.Tests;

/// <summary>Provides configuration, logging, and Google Photos services for integration tests.</summary>
public abstract class TestBase : IDisposable
{
    protected readonly GooglePhotosService _googlePhotosSvc;
    protected readonly ILogger _logger;
    protected readonly ITestOutputHelper _output;
    protected readonly string _testFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata");
    private readonly IConfigurationRoot _configuration;
    private readonly ServiceProvider _serviceProvider;

    protected TestBase(ITestOutputHelper output)
    {
        _output = output;
        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddUserSecrets<TestBase>()
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(_configuration)
            .AddXUnitLogging(output);

        _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();
        services.AddGooglePhotos(_configuration);
        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
        _googlePhotosSvc = _serviceProvider.GetRequiredService<GooglePhotosService>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _serviceProvider.Dispose();
        (_configuration as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
