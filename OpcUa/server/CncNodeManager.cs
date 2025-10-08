using Opc.Ua;
using Opc.Ua.Server;

namespace OpcUa.server;

internal class CncNodeManager : CustomNodeManager2
{
    public CncNodeManager(IServerInternal server, ApplicationConfiguration config, string namespaceUri) :
        base(server, config, namespaceUri)
    {
        SystemContext.NodeIdFactory = this;
    }

    

}