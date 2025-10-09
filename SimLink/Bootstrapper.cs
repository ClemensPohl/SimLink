using MqttBroker;
using OpcUa;

internal static class Bootstrapper
{
    public static IServiceCollection SetupServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddOpcUaServer(config);
        
        services.AddHiveMqClient(config);
        return services;
    }
}