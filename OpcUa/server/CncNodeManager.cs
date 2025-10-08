using System.Reflection;
using Opc.Ua;
using Opc.Ua.Server;
using TypeInfo = Opc.Ua.TypeInfo;

namespace OpcUa.server;

internal class CncNodeManager : CustomNodeManager2
{
    private readonly CncMachine _machine;
    private readonly Dictionary<string, BaseDataVariableState> _varMap = new();
    public CncNodeManager(IServerInternal server, ApplicationConfiguration config, string namespaceUri) :
        base(server, config, namespaceUri)
    {
        SystemContext.NodeIdFactory = this;
        _machine = new CncMachine();
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        var ns = NamespaceIndexes[0];

        var machinesFolder = new BaseObjectState(null)
        {
            SymbolicName = "CncMachine",
            NodeId = new NodeId("CncMachine", ns),
            BrowseName = new QualifiedName("CncMachine", ns),
            DisplayName = "CNC Machine",
            TypeDefinitionId = ObjectTypeIds.BaseObjectType
        };
        AddPredefinedNode(SystemContext, machinesFolder);

        if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var refs))
            externalReferences[ObjectIds.ObjectsFolder] = refs = new List<IReference>();
        if (!refs.Any(r => r.TargetId == machinesFolder.NodeId))
            refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, machinesFolder.NodeId));



        CreateNodesFromObject(_machine, machinesFolder, ns);
    }

    private void CreateNodesFromObject(object source, BaseObjectState parent, ushort ns)
    {
        // Reflect over properties
        foreach (var prop in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var nodeId = new NodeId($"{parent.SymbolicName}.{prop.Name}", ns);

            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = prop.Name,
                NodeId = nodeId,
                BrowseName = new QualifiedName(prop.Name, ns),
                DisplayName = prop.Name,
                DataType = TypeInfo.GetDataTypeId(prop.PropertyType),
                Value = prop.GetValue(source),
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                ValueRank = ValueRanks.Scalar,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            // Handle writes from client → update object
            variable.OnSimpleWriteValue = delegate (ISystemContext context, NodeState node, ref object value)
            {
                prop.SetValue(source, value);
                variable.Value = value;
                variable.Timestamp = DateTime.UtcNow;
                variable.ClearChangeMasks(SystemContext, false);
                return ServiceResult.Good;
            };
            AddPredefinedNode(SystemContext, variable);
            parent.AddChild(variable);
            _varMap[prop.Name] = variable;
        }

        // Reflect over methods
        foreach (var method in source.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                 .Where(m => !m.IsSpecialName))
        {
            var nodeId = new NodeId($"{parent.SymbolicName}.{method.Name}", ns);

            var methodNode = new MethodState(parent)
            {
                SymbolicName = method.Name,
                NodeId = nodeId,
                BrowseName = new QualifiedName(method.Name, ns),
                DisplayName = method.Name,
                Executable = true,
                UserExecutable = true,
                OnCallMethod = (context, m, input, output) =>
                {
                    method.Invoke(source, null);
                    RefreshVariables(); // push any changed values
                    return ServiceResult.Good;
                }
            };

            parent.AddReference(ReferenceTypeIds.HasComponent, false, methodNode.NodeId);
            methodNode.AddReference(ReferenceTypeIds.HasComponent, true, parent.NodeId);
            
            AddPredefinedNode(SystemContext, methodNode);
            parent.AddChild(methodNode);
        }
    }

    private void RefreshVariables()
    {
        var ctx = SystemContext;
        foreach (var kv in _varMap)
        {
            var prop = _machine.GetType().GetProperty(kv.Key)!;
            kv.Value.Value = prop.GetValue(_machine);
            kv.Value.Timestamp = DateTime.UtcNow;
            kv.Value.ClearChangeMasks(ctx, false);
        }
    }
    

}