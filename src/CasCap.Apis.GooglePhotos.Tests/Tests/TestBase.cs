namespace CasCap.Tests;

public abstract class TestBase
{
    protected ILogger _logger;

    protected GooglePhotosService _googlePhotosSvc;

    protected readonly string _testFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testdata/");

    public TestBase(ITestOutputHelper output)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.Test.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<TestBase>()//for local testing
            .AddEnvironmentVariables()//for CI testing
            .Build();

        //initiate ServiceCollection w/logging
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddXUnitLogging(output);

        _logger = ApplicationLogging.LoggerFactory.CreateLogger<TestBase>();

        //add services
        services.AddGooglePhotos();

        //retrieve services
        var serviceProvider = services.BuildServiceProvider();
        _googlePhotosSvc = serviceProvider.GetRequiredService<GooglePhotosService>();
    }
}
