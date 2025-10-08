using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using OpcUa.server;
using OpcUa.settings;

namespace OpcUa.Server;

internal class SimLinkServer : StandardServer
{
    private readonly OpcUaSettings _settings;

    public SimLinkServer(OpcUaSettings settings)
    {
        _settings = settings;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManagers = new List<INodeManager>
        {
            new CncNodeManager(server, configuration, $"{_settings.BaseUrl}/{_settings.Port}/{_settings.AppName}")
        };

        return new MasterNodeManager(server, configuration, null, [.. nodeManagers]);
    }

    protected override ServerProperties LoadServerProperties() => new()
    {
        ManufacturerName = "Pohl-Industries",
        ProductName = "SimLink",
        ProductUri = "uri",
        SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
        BuildNumber = Utils.GetAssemblyBuildNumber(),
        BuildDate = Utils.GetAssemblyTimestamp()
    };
}

/// <summary>
/// Wraps SimLinkServer and manages initialization, configuration, and lifecycle.
/// </summary>
internal class SimLinkServerApp
{
    private readonly SimLinkServer _server;
    private readonly OpcUaSettings _settings;

    public SimLinkServerApp(SimLinkServer server, OpcUaSettings settings)
    {
        _server = server;
        _settings = settings;
    }


    private ApplicationInstance? _application;

    private async Task InitializeAsync()
    {
        _application = new ApplicationInstance
        {
            ApplicationName = _settings.AppName,
            ApplicationType = ApplicationType.Server
        };

        var config = new ApplicationConfiguration
        {
            ApplicationName = _settings.AppName,
            ApplicationUri = $"urn:localhost:{_settings.AppName}",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = BuildSecurityConfig(),
            ServerConfiguration = BuildServerConfig($"{_settings.BaseUrl}/{_settings.Port}/{_settings.AppName}"),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            TraceConfiguration = new TraceConfiguration()
        };

        _application.ApplicationConfiguration = config;

        await config.ValidateAsync(ApplicationType.Server);
        EnsureCertificate(config);
    }

    public async Task StartAsync()
    {
        await InitializeAsync();

        if (_application == null)
            throw new InvalidOperationException("Server not initialized. Call InitializeAsync first.");
            
        var cfg = _application.ApplicationConfiguration.SecurityConfiguration;

        bool haveCert = await _application.CheckApplicationInstanceCertificatesAsync(false, 0);
        if (!haveCert)
            await _application.CheckApplicationInstanceCertificatesAsync(true, 0);

        await _application.StartAsync(_server).ConfigureAwait(false);
    }

    public void Stop()
    {
        _server.Stop();
    }

    private SecurityConfiguration BuildSecurityConfig()
    {
        string basePath = AppContext.BaseDirectory;

        return new SecurityConfiguration
        {
            ApplicationCertificate = new CertificateIdentifier
            {
                StoreType = "Directory",
                StorePath = Path.Combine(basePath, "own"),
                SubjectName = "CN=CNC OPC UA Server, O=Demo, DC=localhost"
            },
            TrustedPeerCertificates = new CertificateTrustList
            {
                StoreType = "Directory",
                StorePath = Path.Combine(basePath, "trusted")
            },
            TrustedIssuerCertificates = new CertificateTrustList
            {
                StoreType = "Directory",
                StorePath = Path.Combine(basePath, "issuers")
            },
            RejectedCertificateStore = new CertificateTrustList
            {
                StoreType = "Directory",
                StorePath = Path.Combine(basePath, "rejected")
            },
            AutoAcceptUntrustedCertificates = true,
            RejectSHA1SignedCertificates = false
        };
    }

    private static ServerConfiguration BuildServerConfig(string endpointUri) => new()
    {
        BaseAddresses = { endpointUri },
        UserTokenPolicies = { new UserTokenPolicy(UserTokenType.Anonymous) },
        SecurityPolicies =
        {
            new ServerSecurityPolicy
            {
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None
            }
        }
    };

    private static void EnsureCertificate(ApplicationConfiguration config)
    {
        if (config.SecurityConfiguration.ApplicationCertificate.Certificate != null)
            return;

        var builder = CertificateFactory.CreateCertificate(
            config.ApplicationUri,
            config.ApplicationName,
            config.SecurityConfiguration.ApplicationCertificate.SubjectName,
            null
        );

        config.SecurityConfiguration.ApplicationCertificate.Certificate = builder
            .SetRSAKeySize(CertificateFactory.DefaultKeySize)
            .CreateForRSA();
    }
}
