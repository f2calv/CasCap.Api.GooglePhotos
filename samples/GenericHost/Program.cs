//This sample shows the typical registration: configuration, logging and dependency injection are all
//provided by the generic host, and AddGooglePhotos wires up both clients, their HttpClient configuration,
//the resilience pipeline and the optional write rate limiter.
//See the ConsoleApp sample for the same flow built by hand.

var builder = Host.CreateApplicationBuilder(args);

//Binds the CasCap:GooglePhotosOptions section and validates it. Credentials come from User Secrets in
//development; see the README for the dotnet user-secrets commands.
builder.Services.AddGooglePhotos(builder.Configuration);

//The worker below does the actual Google Photos work once the host starts.
builder.Services.AddHostedService<GooglePhotosWorker>();

using var host = builder.Build();
await host.RunAsync();
