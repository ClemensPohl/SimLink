namespace MachineDomain;

public class CncMachine
{
    // Static data unchanged during runtime
    public string Name { get; set; } = "PrecisionCraft VMC-850 #3";
    public string Plant { get; set; } = "PlantA";
    public string SerialNumber { get; set; } = "VMC850-2023-003";
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

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        _updateTask = Task.Run(() => Update(token));
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