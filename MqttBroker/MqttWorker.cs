using HiveMQtt.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MqttBroker.Services;

namespace MqttBroker;

internal class MqttWorker : IHostedService
{
    private readonly ILogger<MqttWorker> _logger;
    private readonly HiveMQClient _client;

    private readonly OpcAgent _opcAgent;

    public MqttWorker(HiveMQClient client, ILogger<MqttWorker> logger, OpcAgent opcAgent    )
    {
        _client = client;
        _logger = logger;
        _opcAgent = opcAgent;

    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.ConnectAsync();
            _logger.LogInformation("Client connected sucessfully");

            await _opcAgent.PublishMachinesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Error connecting client to Hivemq {e}", e);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client is null)
            return;

        await _client.DisconnectAsync();
    }
}
