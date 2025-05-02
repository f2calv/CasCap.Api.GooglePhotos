var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddGooglePhotos(builder.Configuration);
builder.Services.AddHostedService<TestBgService>();

IHost host = builder.Build();
host.Run();
