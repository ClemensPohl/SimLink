using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using OpcUa.server;

namespace OpcUa.Server;

internal class SimLinkServer : StandardServer
{
    private readonly string _serverUri;

    public SimLinkServer(string serverUri)
    {
        _serverUri = serverUri;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManagers = new List<INodeManager>
        {
            new CncNodeManager(server, configuration, _serverUri)
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
    private readonly string _basePath;
    private readonly string _endpointUri;

    private ApplicationInstance? _application;
    private SimLinkServer? _server;

    public SimLinkServerApp(string endpointUri, string basePath = "pki")
    {
        _basePath = Path.Combine(AppContext.BaseDirectory, basePath);
        _endpointUri = endpointUri;
    }

    public async Task InitializeAsync(string appName)
    {
        CreatePkiDirectories();

        _application = new ApplicationInstance
        {
            ApplicationName = appName,
            ApplicationType = ApplicationType.Server
        };

        var config = new ApplicationConfiguration
        {
            ApplicationName = appName,
            ApplicationUri = $"urn:localhost:{appName}",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = BuildSecurityConfig(),
            ServerConfiguration = BuildServerConfig(_endpointUri),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            TraceConfiguration = new TraceConfiguration
            {
                OutputFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "opcua.log"),
                DeleteOnLoad = false,
                TraceMasks = 0
            }
        };

        _application.ApplicationConfiguration = config;

        await config.ValidateAsync(ApplicationType.Server);
        EnsureCertificate(config);

        _server = new SimLinkServer(_endpointUri);
    }

    public async Task StartAsync()
    {
        if (_application == null || _server == null)
            throw new InvalidOperationException("Server not initialized. Call InitializeAsync first.");

        var cfg = _application.ApplicationConfiguration.SecurityConfiguration;

        EnsureDirectory(cfg.ApplicationCertificate.StorePath);
        EnsureDirectory(cfg.TrustedPeerCertificates.StorePath);
        EnsureDirectory(cfg.TrustedIssuerCertificates.StorePath);
        EnsureDirectory(cfg.RejectedCertificateStore.StorePath);
        EnsureDirectory(Path.GetDirectoryName(_application.ApplicationConfiguration.TraceConfiguration.OutputFilePath));

        bool haveCert = await _application.CheckApplicationInstanceCertificatesAsync(false, 0);
        if (!haveCert)
            await _application.CheckApplicationInstanceCertificatesAsync(true, 0);

        await _application.StartAsync(_server).ConfigureAwait(false);
    }

    public void Stop()
    {
        _server?.Stop();
        _server = null;
    }

    private void CreatePkiDirectories()
    {
        var dirs = new[]
        {
            Path.Combine(_basePath, "own"),
            Path.Combine(_basePath, "trusted"),
            Path.Combine(_basePath, "issuers"),
            Path.Combine(_basePath, "rejected")
        };

        foreach (var dir in dirs)
            Directory.CreateDirectory(dir);
    }

    private static void EnsureDirectory(string? path)
    {
        if (!string.IsNullOrEmpty(path))
            Directory.CreateDirectory(path);
    }

    private SecurityConfiguration BuildSecurityConfig() => new()
    {
        ApplicationCertificate = new CertificateIdentifier
        {
            StoreType = "Directory",
            StorePath = Path.Combine(_basePath, "own"),
            SubjectName = "CN=CNC OPC UA Server, O=Demo, DC=localhost"
        },
        TrustedPeerCertificates = new CertificateTrustList
        {
            StoreType = "Directory",
            StorePath = Path.Combine(_basePath, "trusted")
        },
        TrustedIssuerCertificates = new CertificateTrustList
        {
            StoreType = "Directory",
            StorePath = Path.Combine(_basePath, "issuers")
        },
        RejectedCertificateStore = new CertificateTrustList
        {
            StoreType = "Directory",
            StorePath = Path.Combine(_basePath, "rejected")
        },
        AutoAcceptUntrustedCertificates = true,
        RejectSHA1SignedCertificates = false
    };

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
