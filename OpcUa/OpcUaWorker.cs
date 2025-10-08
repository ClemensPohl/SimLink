using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpcUa.Server;
using OpcUa.settings;

namespace OpcUa;

internal class OpcUaWorker : IHostedService
{
    private readonly ILogger<OpcUaWorker> _logger;
    private SimLinkServerApp? _server;
    private readonly OpcUaSettings _settings;

    public OpcUaWorker(ILogger<OpcUaWorker> logger, OpcUaSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OpcUa server...");

        try
        {
            _server = new SimLinkServerApp($"{_settings.BaseUrl}/{_settings.Port}/{_settings.AppName}");

            await _server.InitializeAsync(_settings.AppName);

            await _server.StartAsync();

            _logger.LogInformation("Server started");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Stop();
        return Task.CompletedTask;
    }
}