namespace Stackdose.App.PlcDevice.Models;

public sealed class PlcMachineConfig
{
    public string SchemaVersion { get; set; } = "1.0";
    public MachineInfo Machine { get; set; } = new();
    public PlcConnectionConfig Plc { get; set; } = new();
    public PlcTagSection Tags { get; set; } = new();
    public CommandSecurityConfig Security { get; set; } = new();
}

public sealed class MachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
}

public sealed class PlcConnectionConfig
{
    public string Vendor { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public int PollIntervalMs { get; set; } = 300;
    public int TimeoutMs { get; set; } = 1000;
    public int Retry { get; set; } = 2;
}

public sealed class PlcTagSection
{
    public Dictionary<string, PlcTagConfig> Status { get; set; } = new();
    public Dictionary<string, PlcTagConfig> Command { get; set; } = new();
    public Dictionary<string, PlcTagConfig> Process { get; set; } = new();
}

public sealed class PlcTagConfig
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int PulseMs { get; set; }
    public int Length { get; set; } = 1;
    public double Scale { get; set; } = 1.0;
    public string Unit { get; set; } = string.Empty;
}

public sealed class CommandSecurityConfig
{
    public string Start { get; set; } = "Operator";
    public string Stop { get; set; } = "Operator";
    public string Reset { get; set; } = "Supervisor";
}
