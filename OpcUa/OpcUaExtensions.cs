using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpcUa.Server;
using OpcUa.settings;

namespace OpcUa;

public static class OpcUaExtensions
{
    public static IServiceCollection AddOpcUaServer(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpcUaSettings>(config.GetSection("OpcUa"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpcUaSettings>>().Value);

        services.AddSingleton<SimLinkServer>();
        services.AddSingleton<SimLinkServerApp>();

        services.AddHostedService<OpcUaWorker>();
        
        return services;
    }
}