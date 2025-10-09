using System.Text.Json;
using HiveMQtt.Client;
using Microsoft.Extensions.Logging;
using OpcUa.machines;

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
                // 🔹 Publish initial snapshot right away
                await PublishSnapshotAsync(cncMachine, cancellationToken);

                // 🔹 Subscribe to further changes
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
        var topic = $"pohl-industries/{cncMachine.Plant}/machines/{cncMachine.SerialNumber}/telemetry";

        var payloadObj = new
        {
            timestamp = DateTime.UtcNow,
            name = cncMachine.Name,
            plant = cncMachine.Plant,
            serialNumber = cncMachine.SerialNumber,
            phase = cncMachine.Phase.ToString(),
            status = cncMachine.Status.ToString(),
            spindleSpeed = cncMachine.SpindleSpeed
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
