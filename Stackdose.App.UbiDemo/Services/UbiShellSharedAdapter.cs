using Stackdose.App.ShellShared.Models;
using Stackdose.App.UbiDemo.Models;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiShellSharedAdapter
{
    public static UbiAppMeta ToUbiAppMeta(ShellAppMeta source)
    {
        return new UbiAppMeta
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
            NavigationItems = [.. source.NavigationItems.Select(item => new UbiNavigationMetaItem
            {
                Title = item.Title,
                NavigationTarget = item.NavigationTarget,
                RequiredLevel = item.RequiredLevel
            })],
            SoftwareInfoItems = [.. source.SoftwareInfoItems.Select(item => new UbiMetaInfoItem { Label = item.Label, Value = item.Value })]
        };
    }

    public static ShellAppMeta ToShellAppMeta(UbiAppMeta source)
    {
        return new ShellAppMeta
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
            NavigationItems = [.. source.NavigationItems.Select(item => new Stackdose.UI.Core.Shell.ShellNavigationProfileItem(item.Title, item.NavigationTarget, item.RequiredLevel))],
            SoftwareInfoItems = [.. source.SoftwareInfoItems.Select(item => new ShellMetaInfoItem { Label = item.Label, Value = item.Value })]
        };
    }

    public static ShellMachineConfig ToShellMachineConfig(UbiMachineConfig source)
    {
        return new ShellMachineConfig
        {
            Machine = new ShellMachineInfo
            {
                Id = source.Machine.Id,
                Name = source.Machine.Name,
                Enable = source.Machine.Enable
            },
            Plc = new ShellPlcInfo
            {
                Ip = source.Plc.Ip,
                Port = source.Plc.Port,
                PollIntervalMs = source.Plc.PollIntervalMs,
                AutoConnect = source.Plc.AutoConnect
            },
            Tags = new ShellTagSections
            {
                Status = source.Tags.Status.ToDictionary(
                    x => x.Key,
                    x => new ShellTagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase),
                Process = source.Tags.Process.ToDictionary(
                    x => x.Key,
                    x => new ShellTagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase)
            }
        };
    }

    public static UbiMachineConfig ToUbiMachineConfig(ShellMachineConfig source)
    {
        return new UbiMachineConfig
        {
            Machine = new UbiMachineInfo
            {
                Id = source.Machine.Id,
                Name = source.Machine.Name,
                Enable = source.Machine.Enable
            },
            Plc = new UbiPlcInfo
            {
                Ip = source.Plc.Ip,
                Port = source.Plc.Port,
                PollIntervalMs = source.Plc.PollIntervalMs,
                AutoConnect = source.Plc.AutoConnect,
                MonitorAddresses = []
            },
            Tags = new UbiTagSections
            {
                Status = source.Tags.Status.ToDictionary(
                    x => x.Key,
                    x => new UbiTagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase),
                Process = source.Tags.Process.ToDictionary(
                    x => x.Key,
                    x => new UbiTagConfig { Address = x.Value.Address, Type = x.Value.Type, Access = x.Value.Access, Length = x.Value.Length },
                    StringComparer.OrdinalIgnoreCase)
            },
            AlarmConfigFile = string.Empty,
            SensorConfigFile = string.Empty,
            PrintHeadConfigs = [],
            DetailLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }
}
