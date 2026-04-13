using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// Overview Meta ¬M®g ¡X ±N AppMeta ®M¥Î¨ì MachineOverviewPage¡C
/// </summary>
public static class OverviewMetaMapper
{
    public static OverviewMetaMappingResult ApplyMeta(MachineOverviewPage page, AppMeta meta)
    {
        page.ShowMachineCards = meta.ShowMachineCards;
        page.ShowSoftwareInfo = meta.ShowSoftwareInfo;
        page.ShowLiveLog = meta.ShowLiveLog;
        page.BottomPanelHeight = new System.Windows.GridLength(meta.BottomPanelHeight);
        page.BottomLeftTitle = meta.BottomLeftTitle;
        page.BottomRightTitle = meta.BottomRightTitle;
        page.SoftwareInfoItems =
        [
            .. meta.SoftwareInfoItems.Select(item => new OverviewInfoItem(item.Label, item.Value))
        ];

        return new OverviewMetaMappingResult(meta.EnableOverviewAlarmCount);
    }
}

public sealed record OverviewMetaMappingResult(bool EnableOverviewAlarmCount);
