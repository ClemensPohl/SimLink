using System.Reflection;
using MachineDomain;
using Opc.Ua;
using Opc.Ua.Server;
using TypeInfo = Opc.Ua.TypeInfo;

namespace OpcUa.server;

internal class CncNodeManager : CustomNodeManager2, IDisposable
{
    private readonly IEnumerable<CncMachine> _machines;
    private readonly Dictionary<string, BaseDataVariableState> _varMap = [];
    public CncNodeManager(IServerInternal server, ApplicationConfiguration config, string namespaceUri, IEnumerable<CncMachine> machines) :
        base(server, config, namespaceUri)
    {
        SystemContext.NodeIdFactory = this;
        _machines = machines;

        foreach(var machine in _machines)
        {
            machine.MachineStateChanged += RefreshVariables;
        }
    }

    void IDisposable.Dispose()
    {
        foreach(var machine in _machines)
        {
            machine.MachineStateChanged -= RefreshVariables;
        }
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        var ns = NamespaceIndexes[0];

        foreach(var machine in _machines)
        {
            var machinesFolder = new BaseObjectState(null)
            {
                SymbolicName = $"CncMachine - {machine.SerialNumber}",
                NodeId = new NodeId($"CncMachine.{machine.SerialNumber}", ns),
                BrowseName = new QualifiedName($"CncMachine - {machine.SerialNumber}", ns),
                DisplayName = "CNC Machine - " + machine.SerialNumber,
                TypeDefinitionId = ObjectTypeIds.BaseObjectType
            };
            AddPredefinedNode(SystemContext, machinesFolder);

            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var refs))
                externalReferences[ObjectIds.ObjectsFolder] = refs = new List<IReference>();
            if (!refs.Any(r => r.TargetId == machinesFolder.NodeId))
                refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, machinesFolder.NodeId));

            
            CreateNodesFromObject(machine, machinesFolder, ns);
        }


    }

    private void CreateNodesFromObject(object source, BaseObjectState parent, ushort ns)
    {
        // Reflect over properties
        foreach (var prop in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // enum to text conversion:
            var val = prop.GetValue(source);
            var isEnum = prop.PropertyType.IsEnum;
            var dataTypeId = isEnum ? DataTypeIds.String : TypeInfo.GetDataTypeId(prop.PropertyType);
            var displayValue = isEnum ? val?.ToString() : val;

            var nodeId = new NodeId($"{parent.SymbolicName}.{prop.Name}", ns);

            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = prop.Name,
                NodeId = nodeId,
                BrowseName = new QualifiedName(prop.Name, ns),
                DisplayName = prop.Name,
                DataType = dataTypeId,
                Value = displayValue,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                ValueRank = ValueRanks.Scalar,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            // Handle writes from client → update object
            variable.OnSimpleWriteValue = delegate (ISystemContext context, NodeState node, ref object value)

            {
                 if(val == null)
                     return StatusCodes.BadTypeMismatch;

                 object newValue = val;

                if (isEnum)
                {
                    try
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        newValue = Enum.Parse(prop.PropertyType, value.ToString(), ignoreCase: true);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    catch
                    {
                        return StatusCodes.BadTypeMismatch;
                    }
                }
                prop.SetValue(source, newValue);
                variable.Value = isEnum ? newValue.ToString() : newValue;
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
            var prop = _machines.First().GetType().GetProperty(kv.Key);
            if (prop == null)
                continue;

            var value = prop.GetValue(_machines.First());
            var isEnum = prop.PropertyType.IsEnum;
            kv.Value.Value = isEnum ? value?.ToString() : value;
            kv.Value.Timestamp = DateTime.UtcNow;
            kv.Value.ClearChangeMasks(ctx, false);
        }
    }

    

}