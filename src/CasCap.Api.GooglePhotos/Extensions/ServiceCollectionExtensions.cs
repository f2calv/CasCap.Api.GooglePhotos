using Microsoft.Extensions.Http.Resilience;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddGooglePhotos(this IServiceCollection services, IConfiguration configuration, string sectionName = GooglePhotosOptions.ConfigurationSectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GooglePhotosOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddServices();
    }

    public static void AddGooglePhotos(this IServiceCollection services, GooglePhotosOptions googlePhotosOptions)
    {
        ArgumentNullException.ThrowIfNull(googlePhotosOptions);

        services.AddOptions<GooglePhotosOptions>()
            .Configure(options =>
            {
                options.BaseAddress = googlePhotosOptions.BaseAddress;
                options.PickerBaseAddress = googlePhotosOptions.PickerBaseAddress;
                options.User = googlePhotosOptions.User;
                options.Scopes = googlePhotosOptions.Scopes;
                options.ClientId = googlePhotosOptions.ClientId;
                options.ClientSecret = googlePhotosOptions.ClientSecret;
                options.FileDataStoreFullPathOverride = googlePhotosOptions.FileDataStoreFullPathOverride;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddServices();
    }

    public static void AddGooglePhotos(this IServiceCollection services, Action<GooglePhotosOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<GooglePhotosOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddServices();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddHttpClient<GooglePhotosService>((serviceProvider, client) =>
        {
            var googlePhotosOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;
            client.BaseAddress = new Uri(googlePhotosOptions.BaseAddress);
            client.DefaultRequestHeaders.Add("User-Agent", $"{nameof(CasCap)}.{AppDomain.CurrentDomain.FriendlyName}.{Environment.MachineName}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        //https://github.com/aspnet/AspNetCore/issues/6804
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
        .AddStandardResilienceHandler((options) =>
        {
            //RateLimiter
            options.TotalRequestTimeout = new Http.Resilience.HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(90)
            };
            //Retry
            options.Retry = new Http.Resilience.HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 6
            };
            //Circuit Breaker
            options.CircuitBreaker = new Http.Resilience.HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(180)
            };
            //AttemptTimeout
            options.AttemptTimeout = new Http.Resilience.HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(90)
            };
        });

        services.AddHttpClient<GooglePhotosPickerService>((serviceProvider, client) =>
        {
            var googlePhotosOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;
            client.BaseAddress = new Uri(googlePhotosOptions.PickerBaseAddress);
            client.DefaultRequestHeaders.Add("User-Agent", $"{nameof(CasCap)}.{AppDomain.CurrentDomain.FriendlyName}.{Environment.MachineName}");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
    }
}
