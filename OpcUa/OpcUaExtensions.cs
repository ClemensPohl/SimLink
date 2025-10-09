using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpcUa.machines;
using OpcUa.Server;
using OpcUa.settings;

namespace OpcUa;

public static class OpcUaExtensions
{
    public static IServiceCollection AddOpcUaServer(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpcUaSettings>(config.GetSection("OpcUa"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpcUaSettings>>().Value);

        services.AddSingleton(new CncMachine
        {
            Name = "PrecisionCraft VMC-850 #3",
            Plant = "PlantA",
            SerialNumber = "VMC850-2023-003"
        });

        services.AddSingleton(new CncMachine
        {
            Name = "PrecisionCraft VMC-950 #4",
            Plant = "PlantB",
            SerialNumber = "VMC950-2023-004"
        });


        services.AddSingleton<SimLinkServer>();
        services.AddSingleton<SimLinkServerApp>();

        services.AddHostedService<OpcUaWorker>();
        
        return services;
    }
}