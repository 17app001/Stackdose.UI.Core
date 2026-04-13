using Stackdose.UI.Core.Shell;

namespace Stackdose.App.DeviceFramework.Models;

/// <summary>
/// 通用 App Meta — 從 app-meta.json 反序列化。
/// </summary>
public sealed class AppMeta : IShellAppProfile
{
    public string AppId { get; set; } = "DeviceApp";
    public string HeaderDeviceName { get; set; } = "DEVICE";
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
    public List<MetaInfoItem> SoftwareInfoItems { get; set; } = [];
    public List<NavigationMetaItem> NavigationItems { get; set; } = [];

    IReadOnlyList<ShellNavigationProfileItem> IShellAppProfile.NavigationItems
        => NavigationItems
            .Select(item => new ShellNavigationProfileItem(item.Title, item.NavigationTarget, item.RequiredLevel))
            .ToList();
}

public sealed class MetaInfoItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class NavigationMetaItem
{
    public string Title { get; set; } = string.Empty;
    public string NavigationTarget { get; set; } = string.Empty;
    public string RequiredLevel { get; set; } = "Operator";
}
