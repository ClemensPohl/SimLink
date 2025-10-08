// start docker here later
var composeFile = Path.Combine(AppContext.BaseDirectory, "container", "docker-compose.yaml");
Console.WriteLine("composefile location: {0}", composeFile);



var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
builder.Services.SetupServices(builder.Configuration);

var host = builder.Build();
await host.RunAsync();