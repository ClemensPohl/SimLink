namespace OpcUa.server;

public class CncMachine
{
    public double SpindleSpeed { get; set; } = 1000;
    public bool IsRunning { get; set; } = false;

    public void Start()
    {
        SpindleSpeed = 1500;
        IsRunning = true;
    }

    public void Stop() => IsRunning = false;
}