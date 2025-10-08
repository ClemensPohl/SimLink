namespace OpcUa.settings;

internal class OpcUaSettings
{
    public int Port { get; set; }
    public required string AppName { get; set; }
    public required string EndpointUrl { get; set; }
}