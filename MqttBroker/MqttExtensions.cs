
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using HiveMQtt.Client;
using HiveMQtt.Client.Options;
using MqttBroker.Settings;
using MqttBroker.Services;

namespace MqttBroker;

public static class MqttExtensions
{
    public static IServiceCollection AddHiveMqClient(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MqttSettings>(config.GetSection("Mqtt"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MqttSettings>>().Value);

        services.AddSingleton<HiveMQClient>(x =>
        {
            var settings = x.GetRequiredService<MqttSettings>();
            var options = new HiveMQClientOptions
            {
                Host = settings.Host,
                Port = settings.Port,
                ClientId = settings.ClientId
            };
            return new HiveMQClient(options);
        });

        services.AddSingleton<OpcAgent>();

        services.AddHostedService<MqttWorker>();
        
        return services;
    }
}