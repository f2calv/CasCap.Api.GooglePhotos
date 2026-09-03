var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGooglePhotos(builder.Configuration);
builder.Services.AddHostedService<GooglePhotosWorker>();

using var host = builder.Build();
await host.RunAsync();
