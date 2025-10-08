namespace MqttBroker.Settings;

internal class MqttSettings
{
    public required string Host { get; set; }
    public required int Port { get; set; }

    public required string ClientId { get; set; }
}