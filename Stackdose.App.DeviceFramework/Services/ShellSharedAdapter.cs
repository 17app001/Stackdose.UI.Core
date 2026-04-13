using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.ShellShared.Models;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// ShellShared ? DeviceFramework 之間的轉換適配器。
/// </summary>
public static class ShellSharedAdapter
{
    public static AppMeta ToAppMeta(ShellAppMeta source)
    {
        return new AppMeta
        {
            AppId = source.AppId,
            HeaderDeviceName = source.HeaderDeviceName,
            DefaultPageTitle = source.DefaultPageTitle,
            UseFrameworkShellServices = source.UseFrameworkShellServices,
            EnableMetaHotReload = source.EnableMetaHotReload,
            ShowMachineCards = source.ShowMachineCards,
            ShowSoftwareInfo = source.ShowSoftwareInfo,
            ShowLiveLog = source.ShowLiveLog,
            BottomPanelHeight = source.BottomPanelHeight,
            BottomLeftTitle = source.BottomLeftTitle,
            BottomRightTitle = source.BottomRightTitle,
            NavigationItems = [.. source.NavigationItems.Select(item => new NavigationMetaItem
            {
                Title = item.Title,
                NavigationTarget = item.NavigationTarget,
                RequiredLevel = item.RequiredLevel
            })],
            SoftwareInfoItems = [.. source.SoftwareInfoItems.Select(item => new MetaInfoItem { Label = item.Label, Value = item.Value })]
        };
    }

    public static MachineConfig ToMachineConfig(ShellMachineConfig source)
    {
        return new MachineConfig
        {
            Machine = new MachineInfo
            {
                Id = source.Machine.Id,
                Name = source.Machine.Name,
                Enable = source.Machine.Enable
            },
            Plc = new PlcInfo
            {
                Ip = source.Plc.Ip,
                Port = source.Plc.Port,
                PollIntervalMs = source.Plc.PollIntervalMs,
                AutoConnect = source.Plc.AutoConnect,
                MonitorAddresses = []
            },
            Tags = new TagSections
            {
                Status = source.Tags.Status.ToDictionary(
                    x => x.Key,
                    x => new TagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase),
                Process = source.Tags.Process.ToDictionary(
                    x => x.Key,
                    x => new TagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase)
            },
            AlarmConfigFile = string.Empty,
            SensorConfigFile = string.Empty,
            PrintHeadConfigs = [],
            DetailLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
