using Microsoft.Extensions.Http.Resilience;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides dependency-injection registrations for Google Photos API services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers Google Photos services using options bound from configuration.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration containing the Google Photos options section.</param>
    /// <param name="sectionName">The configuration section name to bind.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is <see langword="null" />.</exception>
    public static void AddGooglePhotos(this IServiceCollection services, IConfiguration configuration, string sectionName = GooglePhotosOptions.ConfigurationSectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<GooglePhotosOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateGooglePhotosOptions();
        services.AddServices();
    }

    /// <summary>Registers Google Photos services using a supplied options instance.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="googlePhotosOptions">The options copied into the registered configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="googlePhotosOptions" /> is <see langword="null" />.</exception>
    public static void AddGooglePhotos(this IServiceCollection services, GooglePhotosOptions googlePhotosOptions)
    {
        ArgumentNullException.ThrowIfNull(googlePhotosOptions);

        services.AddOptions<GooglePhotosOptions>()
            .Configure(options =>
            {
                options.BaseAddress = googlePhotosOptions.BaseAddress;
                options.PickerBaseAddress = googlePhotosOptions.PickerBaseAddress;
                options.User = googlePhotosOptions.User;
                options.Scopes = googlePhotosOptions.Scopes is null
                    ? null!
                    : [.. googlePhotosOptions.Scopes];
                options.ClientId = googlePhotosOptions.ClientId;
                options.ClientSecret = googlePhotosOptions.ClientSecret;
                options.FileDataStoreFullPathOverride = googlePhotosOptions.FileDataStoreFullPathOverride;
                options.WriteRateLimit = googlePhotosOptions.WriteRateLimit is null
                    ? null!
                    : new GooglePhotosWriteRateLimitOptions
                    {
                        Enabled = googlePhotosOptions.WriteRateLimit.Enabled,
                        PermitLimit = googlePhotosOptions.WriteRateLimit.PermitLimit,
                        QueueLimit = googlePhotosOptions.WriteRateLimit.QueueLimit,
                        SegmentsPerWindow = googlePhotosOptions.WriteRateLimit.SegmentsPerWindow,
                        WindowSeconds = googlePhotosOptions.WriteRateLimit.WindowSeconds
                    };
            })
            .ValidateGooglePhotosOptions();
        services.AddServices();
    }

    /// <summary>Registers Google Photos services using an options configuration delegate.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">The delegate used to configure Google Photos options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureOptions" /> is <see langword="null" />.</exception>
    public static void AddGooglePhotos(this IServiceCollection services, Action<GooglePhotosOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<GooglePhotosOptions>()
            .Configure(configureOptions)
            .ValidateGooglePhotosOptions();
        services.AddServices();
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<GooglePhotosWriteRateLimitingHandler>();
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
        .AddHttpMessageHandler<GooglePhotosWriteRateLimitingHandler>()
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
            options.Retry.DisableForUnsafeHttpMethods();
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

    private static OptionsBuilder<GooglePhotosOptions> ValidateGooglePhotosOptions(
        this OptionsBuilder<GooglePhotosOptions> optionsBuilder)
        => optionsBuilder
            .ValidateDataAnnotations()
            .Validate(
                options => options.Scopes is { Length: > 0 } && options.Scopes.All(Enum.IsDefined),
                "At least one valid Google Photos OAuth scope is required.")
            .Validate(
                options => options.WriteRateLimit is null
                    || options.WriteRateLimit.SegmentsPerWindow <= options.WriteRateLimit.WindowSeconds,
                "The write rate-limit segments must not exceed the window duration in seconds.")
            .ValidateOnStart();
}
