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

    public double SpindleSpeed { get; set; } = 12000;

    public MachinePhase Phase { get; set; } = MachinePhase.Roughing;
    public MachineStatus Status { get; set; } = MachineStatus.Stopped;


    // Continuous Update Data
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _updateTask;
    public event Action? MachineStateChanged;

    public void StartMachine()
    {
        Status = MachineStatus.Running;
    }

    public void StopMachine()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Stopped;

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        _updateTask = Task.Run(() => Update(token));
    }

    public void EnterMaintainanceMode()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Maintenance;
    }
    
    private async Task Update(CancellationToken token)
    {
        var rand = new Random();
        while (!token.IsCancellationRequested)
        {
            SpindleSpeed = Math.Max(0, SpindleSpeed + rand.Next(-500, 500));
            await Task.Delay(1000, token);
            MachineStateChanged?.Invoke();
        }
    }
}