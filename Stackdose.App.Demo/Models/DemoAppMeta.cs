namespace Stackdose.App.Demo.Models;

public sealed class DemoAppMeta
{
    public string HeaderDeviceName { get; set; } = "DEMO";
    public string DefaultPageTitle { get; set; } = "Machine Overview";
    public bool ShowMachineCards { get; set; } = true;
    public bool ShowSoftwareInfo { get; set; } = true;
    public bool ShowLiveLog { get; set; } = true;
    public double BottomPanelHeight { get; set; } = 340;
    public string BottomLeftTitle { get; set; } = "Software Information";
    public string BottomRightTitle { get; set; } = "Live Log";
    public List<MetaInfoItem> SoftwareInfoItems { get; set; } = [];
}

public sealed class MetaInfoItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
