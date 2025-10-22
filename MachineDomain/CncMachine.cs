namespace MachineDomain;

public class CncMachine
{
    // =======================
    // Public (published to MQTT)
    // =======================

    // Identification
    public string Name { get; set; } = "PrecisionCraft VMC-850 #3";
    public string SerialNumber { get; set; } = "VMC850-2023-003";
    public string Plant { get; set; } = "Munich Precision Manufacturing";
    public string ProductionSegment { get; set; } = "Aerospace Components";
    public string ProductionLine { get; set; } = "5-Axis Machining Cell C";

    // Current Production
    public string ProductionOrder { get; set; } = "PO-2024-AERO-0876";
    public string Article { get; set; } = "ART-TB-7075-T6";

    // Machine Status
    public MachineStatus Status { get; set; } = MachineStatus.Running;
    public MachinePhase Phase { get; set; } = MachinePhase.Roughing;

    // Live Telemetry
    public double ActualSpindleSpeed { get; set; } = 29487;
    public string ActualFeedRate { get; set; } = "1198.5 mm/min";
    public string CoolantTemperature { get; set; } = "22.5 °C";
    public string ActualCycleTime { get; set; } = "73.2 seconds";

    // Production Counters
    public double GoodParts { get; set; } = 0;
    public double BadParts { get; set; } = 0;
    public double TotalParts { get; set; } = 0;

    /// <summary>
    /// Production progress as percentage (0.00 - 100.00). Values are clamped to [0,100],
    /// rounded to 2 decimal places, and NaN/Infinity are rejected.
    /// </summary>
    private double _productionOrderProgress = 0.0;
    public double ProductionOrderProgress
    {
        get => _productionOrderProgress;
        set
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("ProductionOrderProgress must be a finite number.");

            var clamped = Math.Max(0.0, Math.Min(100.0, value));
            // Round to 2 decimal places for a nice percentage
            _productionOrderProgress = Math.Round(clamped, 2, MidpointRounding.AwayFromZero);
        }
    } 

    // =======================
    // Private fields (not published to MQTT)
    // =======================

    // --- Target setpoints (desired values) ---
    private double? _targetSpindleSpeed;
    private double? _targetFeedRate;
    private double? _targetCoolantFlow;
    private double? _targetCycleTime;
    private double? _targetSurfaceFinish;

    // --- Actual measurements ---
    private string? _actualCoolantFlow;
    private string? _actualSurfaceFinish;

    // --- Tooling ---
    private string? _currentToolNumber;
    private string? _toolLifeRemaining;

    // --- Axis positions ---
    private double _xAxisPosition = 0;
    private double _yAxisPosition = 0;
    private double _zAxisPosition = 0;

    // --- Cutting forces ---
    private string? _cuttingForceX;
    private string? _cuttingForceY;
    private string? _cuttingForceZ;


    // Continuous Update Data
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _updateTask;
    public event Action? MachineStateChanged;

    public void StartMachine()
    {
        Status = MachineStatus.Starting;
        MachineStateChanged?.Invoke();

        Task.Delay(2000).Wait(); // Simulate startup delay

        Status = MachineStatus.Running;
        MachineStateChanged?.Invoke();


        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        _updateTask = Task.Run(() => Update(token));
    }

    public void StopMachine()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Stopped;
        _cancellationTokenSource?.Cancel();
        _updateTask = null;
    }

    public void EnterMaintainanceMode()
    {
        if (Status == MachineStatus.Stopped)
            return;

        Status = MachineStatus.Maintenance;
    }

    public void ResetCounters()
    {
        GoodParts = 0;
        BadParts = 0;
        TotalParts = 0;
        ProductionOrderProgress = 0;
        MachineStateChanged?.Invoke();
    }
    public void ResetHomeAxis()
    {
        _xAxisPosition = 0;
        _yAxisPosition = 0;
        _zAxisPosition = 0;
        MachineStateChanged?.Invoke();
    }


    // methods with parameters we need to add that reflection based in the nodemanager as we only accept parameterless methods for now
    public void LoadProductionOrder(string orderNumber, string article, int targetQuantity,
        double targetSpindleSpeed, double targetFeedRate, double targetSurfaceFinish, double targetCycleTime, double targetCoolantFlow)
    {
        ProductionOrder = orderNumber;
        Article = article;
        _targetSpindleSpeed = targetSpindleSpeed;
        _targetFeedRate = targetFeedRate;
        _targetSurfaceFinish = targetSurfaceFinish;
        _targetCycleTime = targetCycleTime;
        _targetCoolantFlow = targetCoolantFlow;

        ResetCounters();
    }

    public void ToolChange(string toolNumber)
    {
        _currentToolNumber = toolNumber;
        MachineStateChanged?.Invoke();
    }

    
    private async Task Update(CancellationToken token)
    {
        var rand = new Random();
        while (!token.IsCancellationRequested)
        {
            ActualSpindleSpeed = Math.Max(0, ActualSpindleSpeed + rand.Next(-500, 500));
            await Task.Delay(1000, token);
            MachineStateChanged?.Invoke();
        }
    }
}