using Stackdose.App.UbiDemo.Models;
using Stackdose.App.ShellShared.Services;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeLoader
{
    public static List<UbiMachineConfig> LoadMachines(string configDirectory)
    {
        var sharedBaseline = ShellConfigLoader.LoadMachines(configDirectory)
            .Select(UbiShellSharedAdapter.ToUbiMachineConfig)
            .ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(configDirectory))
        {
            return [.. sharedBaseline.Values.Where(x => x.Machine.Enable)];
        }

        var files = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<UbiMachineConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config?.Machine.Enable == true)
                {
                    sharedBaseline[config.Machine.Id] = config;
                }
            }
            catch
            {
            }
        }

        return [.. sharedBaseline.Values.Where(x => x.Machine.Enable)];
    }

    public static UbiAppMeta LoadMeta(string filePath)
    {
        var sharedMeta = UbiShellSharedAdapter.ToUbiAppMeta(ShellAppMetaLoader.Load(filePath));

        try
        {
            if (!File.Exists(filePath))
            {
                return sharedMeta;
            }

            var json = File.ReadAllText(filePath);
            var meta = JsonSerializer.Deserialize<UbiAppMeta>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (meta is null)
            {
                return sharedMeta;
            }

            return MergeMeta(sharedMeta, meta);
        }
        catch
        {
            return sharedMeta;
        }
    }

    private static UbiAppMeta MergeMeta(UbiAppMeta baseline, UbiAppMeta overlay)
    {
        return new UbiAppMeta
        {
            AppId = string.IsNullOrWhiteSpace(overlay.AppId) ? baseline.AppId : overlay.AppId,
            HeaderDeviceName = string.IsNullOrWhiteSpace(overlay.HeaderDeviceName) ? baseline.HeaderDeviceName : overlay.HeaderDeviceName,
            DefaultPageTitle = string.IsNullOrWhiteSpace(overlay.DefaultPageTitle) ? baseline.DefaultPageTitle : overlay.DefaultPageTitle,
            UseFrameworkShellServices = overlay.UseFrameworkShellServices,
            EnableMetaHotReload = overlay.EnableMetaHotReload,
            EnableOverviewAlarmCount = overlay.EnableOverviewAlarmCount,
            ShowMachineCards = overlay.ShowMachineCards,
            ShowSoftwareInfo = overlay.ShowSoftwareInfo,
            ShowLiveLog = overlay.ShowLiveLog,
            BottomPanelHeight = overlay.BottomPanelHeight > 0 ? overlay.BottomPanelHeight : baseline.BottomPanelHeight,
            BottomLeftTitle = string.IsNullOrWhiteSpace(overlay.BottomLeftTitle) ? baseline.BottomLeftTitle : overlay.BottomLeftTitle,
            BottomRightTitle = string.IsNullOrWhiteSpace(overlay.BottomRightTitle) ? baseline.BottomRightTitle : overlay.BottomRightTitle,
            SoftwareInfoItems = overlay.SoftwareInfoItems.Count > 0 ? overlay.SoftwareInfoItems : baseline.SoftwareInfoItems,
            NavigationItems = overlay.NavigationItems.Count > 0 ? overlay.NavigationItems : baseline.NavigationItems
        };
    }
}
