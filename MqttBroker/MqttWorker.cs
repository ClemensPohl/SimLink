using HiveMQtt.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MqttBroker;

public class MqttWorker : IHostedService
{
    private readonly ILogger<MqttWorker> _logger;
    private readonly HiveMQClient _client;

    public MqttWorker(HiveMQClient client, ILogger<MqttWorker> logger)
    {
        _client = client;
        _logger = logger;

    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.ConnectAsync();
            _logger.LogInformation("Client connected sucessfully");
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
