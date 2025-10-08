using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpcUa.Server;

namespace OpcUa;

public class OpcUaWorker : IHostedService
{
    private readonly ILogger<OpcUaWorker> _logger;
    private SimLinkServerApp? _server;

    public OpcUaWorker(ILogger<OpcUaWorker> logger)
    {
        _logger = logger;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OpcUa server...");

        try
        {
            _server = new SimLinkServerApp("opc.tcp://localhost:4840/SimLink");

            await _server.InitializeAsync("SimLink", 4840);

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