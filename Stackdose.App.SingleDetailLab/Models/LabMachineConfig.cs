namespace Stackdose.App.SingleDetailLab.Models;

public sealed class LabMachineConfig
{
    public LabMachineInfo Machine { get; set; } = new();
    public LabPlcInfo Plc { get; set; } = new();
    public LabTagSections Tags { get; set; } = new();
}

public sealed class LabMachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
}

public sealed class LabPlcInfo
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
    public int PollIntervalMs { get; set; } = 150;
    public bool AutoConnect { get; set; } = true;
}

public sealed class LabTagSections
{
    public Dictionary<string, LabTagConfig> Status { get; set; } = new();
    public Dictionary<string, LabTagConfig> Process { get; set; } = new();
}

public sealed class LabTagConfig
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int Length { get; set; } = 1;
}
