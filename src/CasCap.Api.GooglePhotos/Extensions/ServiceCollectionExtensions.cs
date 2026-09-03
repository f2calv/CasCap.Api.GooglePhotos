using Microsoft.Extensions.Http.Resilience;
using Polly;
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
                options.RequestTimeoutSeconds = googlePhotosOptions.RequestTimeoutSeconds;
                options.UploadTimeoutSeconds = googlePhotosOptions.UploadTimeoutSeconds;
                options.WriteRateLimit = googlePhotosOptions.WriteRateLimit is null
                    ? null!
                    : googlePhotosOptions.WriteRateLimit with { };
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
        .AddStandardResilienceHandler()
        .Configure((options, serviceProvider) => ConfigureResilience(
            options,
            serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value,
            uploadAware: true));

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
        .AddStandardResilienceHandler()
        .Configure((options, serviceProvider) => ConfigureResilience(
            options,
            serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value,
            uploadAware: false));
    }

    private static void ConfigureResilience(
        HttpStandardResilienceOptions options,
        GooglePhotosOptions googlePhotosOptions,
        bool uploadAware)
    {
        var requestTimeout = TimeSpan.FromSeconds(googlePhotosOptions.RequestTimeoutSeconds);
        var uploadTimeout = TimeSpan.FromSeconds(googlePhotosOptions.UploadTimeoutSeconds);

        options.Retry.MaxRetryAttempts = 6;
        options.Retry.DisableForUnsafeHttpMethods();

        options.AttemptTimeout.Timeout = requestTimeout;

        //The previous total budget equalled a single attempt, so retries could never complete.
        var totalTimeout = requestTimeout * (options.Retry.MaxRetryAttempts + 1);
        options.TotalRequestTimeout.Timeout = totalTimeout;

        //The standard handler rejects a sampling duration shorter than two attempts.
        options.CircuitBreaker.SamplingDuration = requestTimeout * 2;

        if (!uploadAware)
            return;

        //Uploads stream whole files or large chunks, so they need their own budget rather than the API request timeout.
        options.AttemptTimeout.TimeoutGenerator = args => SelectTimeout(args.Context, requestTimeout, uploadTimeout);
        options.TotalRequestTimeout.TimeoutGenerator = args => SelectTimeout(args.Context, totalTimeout, uploadTimeout);
    }

    private static ValueTask<TimeSpan> SelectTimeout(ResilienceContext context, TimeSpan requestTimeout, TimeSpan uploadTimeout)
    {
        var request = context.GetRequestMessage();
        return ValueTask.FromResult(request is not null && UploadHeaders.IsUploadRequest(request)
            ? uploadTimeout
            : requestTimeout);
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
