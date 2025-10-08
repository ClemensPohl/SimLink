using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpcUa.Server;

namespace OpcUa;

internal class OpcUaWorker : IHostedService
{
    private readonly ILogger<OpcUaWorker> _logger;
    private readonly SimLinkServerApp _server;

    public OpcUaWorker(ILogger<OpcUaWorker> logger, SimLinkServerApp server)
    {
        _logger = logger;
        _server = server;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting OpcUa server...");

        try
        {
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