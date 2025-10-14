using HiveMQtt.Client;
using Microsoft.Extensions.Logging;
using OpcUa.machines;
using System.Text.Json;

namespace MqttBroker.Services;

public class OpcAgent
{
    private readonly IEnumerable<CncMachine> _cncMachines;
    private readonly HiveMQClient _client;
    private readonly ILogger<OpcAgent> _logger;

    public OpcAgent(IEnumerable<CncMachine> cncMachines, HiveMQClient client, ILogger<OpcAgent> logger)
    {
        _cncMachines = cncMachines;
        _client = client;
        _logger = logger;
    }

    public async Task PublishMachinesAsync(CancellationToken cancellationToken)
    {
        foreach (var cncMachine in _cncMachines)
        {
            try
            {
                // Publish initial snapshot right away
                await PublishSnapshotAsync(cncMachine, cancellationToken);

                // Subscribe to further changes
                cncMachine.MachineStateChanged += async () =>
                {
                    await PublishSnapshotAsync(cncMachine, cancellationToken);
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to CNC Machine {Name}", cncMachine.Name);
            }
        }
    }

    private async Task PublishSnapshotAsync(CncMachine cncMachine, CancellationToken token)
    {
        string baseTopic = $"pohl-industries/{cncMachine.Plant}/machines/{cncMachine.SerialNumber}/telemetry";

        var metrics = new Dictionary<string, object?>
        {
            // INFO 
            ["info/serialNumber"] = cncMachine.SerialNumber,
            ["info/name"] = cncMachine.Name,
            ["info/plant"] = cncMachine.Plant,

            // RUNTIME
            ["runtime/spindleSpeed"] = cncMachine.SpindleSpeed,

            // STATUS
            ["status/state"] = cncMachine.Status.ToString(),
            ["status/phase"] = cncMachine.Phase.ToString()
        };

        foreach (var metric in metrics)
        {
            string topic = $"{baseTopic}/{metric.Key}";

            // build small JSON payload
            var payloadObj = new
            {
                timestamp = DateTime.UtcNow,
                value = metric.Value
            };

            string payload = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            try
            {
                await _client.PublishAsync(topic, payload);
                _logger.LogInformation("Published machine snapshot to {Topic}: {Payload}", topic, payload);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish snapshot for machine {Serial}", cncMachine.SerialNumber);
            }
        }
    }
}
