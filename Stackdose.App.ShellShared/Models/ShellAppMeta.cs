using Stackdose.UI.Core.Shell;

namespace Stackdose.App.ShellShared.Models;

public sealed class ShellAppMeta : IShellAppProfile
{
    public string AppId { get; set; } = "ShellApp";
    public string HeaderDeviceName { get; set; } = "SHELL";
    public string DefaultPageTitle { get; set; } = "Machine Overview";
    public bool UseFrameworkShellServices { get; set; } = false;
    public bool EnableMetaHotReload { get; set; } = false;
    public bool ShowMachineCards { get; set; } = true;
    public bool ShowSoftwareInfo { get; set; } = true;
    public bool ShowLiveLog { get; set; } = true;
    public double BottomPanelHeight { get; set; } = 340;
    public string BottomLeftTitle { get; set; } = "Software Information";
    public string BottomRightTitle { get; set; } = "Live Log";
    public ShellDetailPageProfile DetailPage { get; set; } = new();
    public List<ShellMetaInfoItem> SoftwareInfoItems { get; set; } = [];
    public List<ShellNavigationProfileItem> NavigationItems { get; set; } = [];

    IReadOnlyList<ShellNavigationProfileItem> IShellAppProfile.NavigationItems => NavigationItems;
}

public sealed class ShellDetailPageProfile
{
    public string PageTitle { get; set; } = "Machine Detail";
    public string BatchTagSection { get; set; } = "process";
    public string BatchTagKey { get; set; } = "batchNo";
    public string RecipeTagSection { get; set; } = "process";
    public string RecipeTagKey { get; set; } = "recipeNo";
    public string MachineStateTagSection { get; set; } = "status";
    public string MachineStateTagKey { get; set; } = "isRunning";
    public string AlarmStateTagSection { get; set; } = "status";
    public string AlarmStateTagKey { get; set; } = "isAlarm";
    public string NozzleTempTagSection { get; set; } = "process";
    public string NozzleTempTagKey { get; set; } = "nozzleTemp";
}

public sealed class ShellMetaInfoItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
