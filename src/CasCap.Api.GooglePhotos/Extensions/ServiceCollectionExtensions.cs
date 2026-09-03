using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the Google Photos Library and Picker API clients.
/// Follows official best practice/guidance from Microsoft for library authors,
/// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors"/>.
/// </summary>
/// <remarks>
/// Note: Official documentation says to not add these extension methods to the
/// <see cref="DependencyInjection"/> namespace however we are opting to ignore that recommendation!
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers Google Photos services using options bound from configuration.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration containing the Google Photos options section.</param>
    /// <param name="sectionName">The configuration section name to bind. Defaults to <see cref="GooglePhotosOptions.ConfigurationSectionName"/>.</param>
    /// <returns>The same service collection so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddGooglePhotos(this IServiceCollection services, IConfiguration configuration, string? sectionName = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        //Resolved here rather than as a default parameter value, which would be baked into consumer assemblies.
        sectionName ??= GooglePhotosOptions.ConfigurationSectionName;
        services.AddOptions<GooglePhotosOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateGooglePhotosOptions();
        return services.AddServices();
    }

    /// <summary>Registers Google Photos services using a supplied options instance.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="googlePhotosOptions">The options copied into the registered configuration.</param>
    /// <returns>The same service collection so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="googlePhotosOptions" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddGooglePhotos(this IServiceCollection services, GooglePhotosOptions googlePhotosOptions)
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
        return services.AddServices();
    }

    /// <summary>Registers Google Photos services using an options configuration delegate.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureOptions">The delegate used to configure Google Photos options.</param>
    /// <returns>The same service collection so that calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configureOptions" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddGooglePhotos(this IServiceCollection services, Action<GooglePhotosOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<GooglePhotosOptions>()
            .Configure(configureOptions)
            .ValidateGooglePhotosOptions();
        return services.AddServices();
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        //Options configuration accumulates across repeated calls, but the clients and handlers must only be wired once.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(GooglePhotosCredentialProvider)))
            return services;

        services.TryAddSingleton<GooglePhotosCredentialProvider>();
        services.TryAddTransient<GooglePhotosAuthorizationHandler>();
        services.TryAddTransient<GooglePhotosWriteRateLimitingHandler>();

        var libraryBuilder = services.AddHttpClient<GooglePhotosService>((serviceProvider, client) =>
        {
            var googlePhotosOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;
            client.BaseAddress = new Uri(googlePhotosOptions.BaseAddress);
            ConfigureCommonHeaders(client);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
        //https://github.com/aspnet/AspNetCore/issues/6804
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
        //Registered outside the resilience handler, so a retried write does not re-acquire a permit.
        //Harmless while retries are disabled for unsafe methods, which are the only methods the limiter gates.
        .AddHttpMessageHandler<GooglePhotosWriteRateLimitingHandler>();
        libraryBuilder.AddStandardResilienceHandler()
            .Configure((options, serviceProvider) => ConfigureResilience(
                options,
                serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value,
                uploadAware: true));
        //Registered last so that it runs innermost and every retried attempt picks up a freshly refreshed token.
        libraryBuilder.AddHttpMessageHandler<GooglePhotosAuthorizationHandler>();

        var pickerBuilder = services.AddHttpClient<GooglePhotosPickerService>((serviceProvider, client) =>
        {
            var googlePhotosOptions = serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value;
            client.BaseAddress = new Uri(googlePhotosOptions.PickerBaseAddress);
            ConfigureCommonHeaders(client);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });
        pickerBuilder.AddStandardResilienceHandler()
            .Configure((options, serviceProvider) => ConfigureResilience(
                options,
                serviceProvider.GetRequiredService<IOptions<GooglePhotosOptions>>().Value,
                uploadAware: false));
        pickerBuilder.AddHttpMessageHandler<GooglePhotosAuthorizationHandler>();

        return services;
    }

    private static void ConfigureCommonHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("User-Agent", $"{nameof(CasCap)}.{AppDomain.CurrentDomain.FriendlyName}.{Environment.MachineName}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        //Request timeouts are owned by the resilience pipeline, which distinguishes uploads from ordinary requests.
        client.Timeout = Timeout.InfiniteTimeSpan;
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
