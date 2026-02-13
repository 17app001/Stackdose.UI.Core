using Stackdose.App.PlcDevice.Models;

namespace Stackdose.App.PlcDevice.Services;

public sealed class PlcMachineRuntime
{
    private readonly Random _random = new();
    private int _heartbeat;

    public PlcMachineRuntime(PlcMachineConfig config)
    {
        Config = config;
        MachineState = "Idle";
        AlarmState = "Normal";
        BatchNumber = $"B-{DateTime.Now:yyyyMMdd}-01";
        RecipeName = "Recipe-A01";
        NozzleTempC = 24.8;
    }

    public PlcMachineConfig Config { get; }
    public bool IsRunning { get; private set; }
    public bool IsAlarm { get; private set; }
    public bool IsConnected { get; private set; }
    public string MachineState { get; private set; }
    public string AlarmState { get; private set; }
    public int Heartbeat => _heartbeat;
    public string BatchNumber { get; private set; }
    public string RecipeName { get; private set; }
    public double NozzleTempC { get; private set; }

    public void Tick()
    {
        _heartbeat++;

        if (IsRunning)
        {
            NozzleTempC = Math.Min(74.5, NozzleTempC + _random.NextDouble() * 0.5);
        }
        else
        {
            NozzleTempC = Math.Max(24.0, NozzleTempC - _random.NextDouble() * 0.4);
        }

        if (_heartbeat % 120 == 0)
        {
            IsAlarm = _random.Next(0, 10) == 0;
            AlarmState = IsAlarm ? "Alarm Active" : "Normal";

            if (IsAlarm)
            {
                IsRunning = false;
                MachineState = "Alarm";
            }
        }
    }

    public void ApplySnapshot(bool isConnected, bool isRunning, bool isAlarm, int heartbeat, string mode, double? nozzleTemp, string? batchNo = null, string? recipeNo = null)
    {
        IsConnected = isConnected;
        IsRunning = isRunning;
        IsAlarm = isAlarm;
        _heartbeat = heartbeat;

        MachineState = isAlarm
            ? "Alarm"
            : isRunning
                ? "Running"
                : mode;

        AlarmState = isAlarm ? "Alarm Active" : "Normal";

        if (nozzleTemp.HasValue)
        {
            NozzleTempC = nozzleTemp.Value;
        }

        if (!string.IsNullOrWhiteSpace(batchNo))
        {
            BatchNumber = batchNo;
        }

        if (!string.IsNullOrWhiteSpace(recipeNo))
        {
            RecipeName = recipeNo;
        }
    }

    public bool Start()
    {
        if (IsAlarm)
        {
            return false;
        }

        IsRunning = true;
        MachineState = "Running";
        return true;
    }

    public void Stop()
    {
        IsRunning = false;
        MachineState = "Stopped";
    }

    public void Reset()
    {
        IsAlarm = false;
        AlarmState = "Normal";
        IsRunning = false;
        MachineState = "Idle";
    }
}
