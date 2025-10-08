using MqttBroker;
using OpcUa;

internal static class Bootstrapper
{
    public static IServiceCollection SetupServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHiveMqClient(config);
        services.AddOpcUaServer(config);
        return services;
    }
}