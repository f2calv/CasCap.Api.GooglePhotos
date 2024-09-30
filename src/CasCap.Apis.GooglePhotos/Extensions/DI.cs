using Polly;
using System.Net;
using System.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
    public static void AddGooglePhotos(this IServiceCollection services, IConfiguration configuration, string sectionKey = GooglePhotosOptions.SectionKey)
        => services.AddServices(configuration: configuration, sectionKey: sectionKey);

    public static void AddGooglePhotos(this IServiceCollection services, GooglePhotosOptions googlePhotosOptions)
        => services.AddServices(googlePhotosOptions: googlePhotosOptions);

    public static void AddGooglePhotos(this IServiceCollection services, Action<GooglePhotosOptions> configureOptions)
        => services.AddServices(configureOptions: configureOptions);

    static void AddServices(this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionKey = GooglePhotosOptions.SectionKey,
        GooglePhotosOptions? googlePhotosOptions = null,
        Action<GooglePhotosOptions>? configureOptions = null
        )
    {
        if (configuration is not null)
        {
            var configSection = configuration.GetSection(sectionKey);
            googlePhotosOptions = configSection.Get<GooglePhotosOptions>();
            if (googlePhotosOptions is not null)
                services.Configure<GooglePhotosOptions>(configSection);
        }
        else if (googlePhotosOptions is not null)
        {
            var options = Options.Options.Create(googlePhotosOptions);
            services.AddSingleton(options);
        }
        else if (configureOptions is not null)
        {
            services.Configure(configureOptions);
            googlePhotosOptions = new();
            configureOptions.Invoke(googlePhotosOptions);
        }
        if (googlePhotosOptions is null)
            throw new GooglePhotosException($"configuration object {nameof(GooglePhotosOptions)} is null so cannot continue");

        services.AddHttpClient<GooglePhotosService>((s, client) =>
        {
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
            options.Retry = new Http.Resilience.HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 6
            };
        });
    }
}
