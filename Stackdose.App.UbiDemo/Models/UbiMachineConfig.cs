using Stackdose.UI.Core.Shell;

namespace Stackdose.App.UbiDemo.Models;

public sealed class UbiMachineConfig
{
    public UbiMachineInfo Machine { get; set; } = new();
    public UbiPlcInfo Plc { get; set; } = new();
    public UbiTagSections Tags { get; set; } = new();
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public List<string> PrintHeadConfigs { get; set; } = [];
    public Dictionary<string, string> DetailLabels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class UbiMachineInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enable { get; set; } = true;
}

public sealed class UbiPlcInfo
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5000;
    public int PollIntervalMs { get; set; } = 300;
    public bool AutoConnect { get; set; } = true;
    public List<string> MonitorAddresses { get; set; } = [];
}

public sealed class UbiTagSections
{
    public Dictionary<string, UbiTagConfig> Status { get; set; } = new();
    public Dictionary<string, UbiTagConfig> Process { get; set; } = new();
}

public sealed class UbiTagConfig
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
    public int Length { get; set; } = 1;
}

public sealed class UbiAppMeta : IShellAppProfile
{
    public string AppId { get; set; } = "UbiDemo";
    public string HeaderDeviceName { get; set; } = "UBI";
    public string DefaultPageTitle { get; set; } = "Machine Overview";
    public bool UseFrameworkShellServices { get; set; } = false;
    public bool EnableMetaHotReload { get; set; } = false;
    public bool EnableOverviewAlarmCount { get; set; } = false;
    public bool ShowMachineCards { get; set; } = true;
    public bool ShowSoftwareInfo { get; set; } = true;
    public bool ShowLiveLog { get; set; } = true;
    public double BottomPanelHeight { get; set; } = 440;
    public string BottomLeftTitle { get; set; } = "Software Information";
    public string BottomRightTitle { get; set; } = "Live Log";
    public List<UbiMetaInfoItem> SoftwareInfoItems { get; set; } = [];
    public List<UbiNavigationMetaItem> NavigationItems { get; set; } = [];

    IReadOnlyList<ShellNavigationProfileItem> IShellAppProfile.NavigationItems
        => NavigationItems
            .Select(item => new ShellNavigationProfileItem(item.Title, item.NavigationTarget, item.RequiredLevel))
            .ToList();
}

public sealed class UbiMetaInfoItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class UbiNavigationMetaItem
{
    public string Title { get; set; } = string.Empty;
    public string NavigationTarget { get; set; } = string.Empty;
    public string RequiredLevel { get; set; } = "Operator";
}

public sealed class DeviceContext
{
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string BatchAddress { get; set; } = "--";
    public string RecipeAddress { get; set; } = "--";
    public string NozzleAddress { get; set; } = "--";
    public string RunningAddress { get; set; } = "--";
    public string AlarmAddress { get; set; } = "--";
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public string PrintHead1ConfigFile { get; set; } = string.Empty;
    public string PrintHead2ConfigFile { get; set; } = string.Empty;

    public string TotalTrayAddress { get; set; } = "D3400";
    public string CurrentTrayAddress { get; set; } = "D33";
    public string TotalLayerAddress { get; set; } = "D3401";
    public string CurrentLayerAddress { get; set; } = "D32";
    public string SwitchGraphicLayerAddress { get; set; } = "D510";
    public string SwitchAreaLayerAddress { get; set; } = "D512";
    public string MessageIdAddress { get; set; } = "D85";
    public string BatteryAddress { get; set; } = "D120";
    public string ElapsedTimeAddress { get; set; } = "D86";
    public string PrintHeadCountAddress { get; set; } = "D87";
}
