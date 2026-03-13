using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiOverviewMetaMapper
{
    public static UbiOverviewMetaMappingResult ApplyMeta(MachineOverviewPage page, UbiAppMeta meta)
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

        return new UbiOverviewMetaMappingResult(meta.EnableOverviewAlarmCount);
    }
}
