namespace Stackdose.App.ShellShared.Models;

public sealed class ShellMachineConfig
{
    public ShellMachineInfo Machine { get; set; } = new();
    public ShellPlcInfo Plc { get; set; } = new();
    public ShellTagSections Tags { get; set; } = new();
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
}

public sealed class ShellMachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
}

public sealed class ShellPlcInfo
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
    public int PollIntervalMs { get; set; } = 300;
    public bool AutoConnect { get; set; } = true;
}

public sealed class ShellTagSections
{
    public Dictionary<string, ShellTagConfig> Status { get; set; } = new();
    public Dictionary<string, ShellTagConfig> Process { get; set; } = new();
}

public sealed class ShellTagConfig
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int Length { get; set; } = 1;
}
