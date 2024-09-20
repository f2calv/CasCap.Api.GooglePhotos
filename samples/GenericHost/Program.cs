﻿namespace CasCap;

public class Program
{
    static readonly string _environmentName = "Development";

    public static void Main(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseEnvironment(_environmentName)
            .ConfigureAppConfiguration((hostContext, configBuilder) =>
            {
                configBuilder.AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true);
                configBuilder.AddJsonFile($"appsettings.{_environmentName}.json", optional: true, reloadOnChange: true);
                if (hostContext.HostingEnvironment.IsDevelopment())
                    configBuilder.AddUserSecrets<Program>();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddGooglePhotos(hostContext.Configuration);
                services.AddHostedService<MyBackgroundService>();
            })
            .Build().Run();
}
