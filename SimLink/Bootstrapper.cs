using MqttBroker;
using OpcUa;

internal static class Bootstrapper
{
    public static IServiceCollection SetupServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHiveMqClient(config);
        // OpcUa
        services.AddHostedService<OpcUaWorker>();
        return services;
    }
}