namespace OpcUa.machines;

public class CncMachine
{
    private string Id { get; } = Guid.NewGuid().ToString();

    // Static data unchanged during runtime
    public string Name { get; set; } = "PrecisionCraft VMC-850 #3";
    public string Plant { get; set; } = "Sulz City";
    private string SerialNumber { get; set; } = "VMC850-2023-003";
    private string ProductionSegment { get; set; } = "Aerospace Components";
    private string ProductionLine { get; set; } = "5-Axis Machining Cell C";

    public MachinePhase Phase { get; set; } = MachinePhase.Roughing;
    public MachineStatus Status { get; set; } = MachineStatus.Running;

    public void StartMachine()
    {
        Status = MachineStatus.Running;
    }

    public void StopMachine()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Stopped;
    }
    
    public void EnterMaintainanceMode()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Maintenance;
    }
}