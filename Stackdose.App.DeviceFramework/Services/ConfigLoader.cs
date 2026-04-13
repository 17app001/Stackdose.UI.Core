using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.ShellShared.Services;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Config 載入器 — 讀取 app-meta.json + Machine*.config.json。
/// </summary>
public static class ConfigLoader
{
    public static List<MachineConfig> LoadMachines(string configDirectory)
    {
        var result = new Dictionary<string, MachineConfig>(StringComparer.OrdinalIgnoreCase);

        // 從 ShellShared 取得基本 baseline
        var sharedConfigs = ShellConfigLoader.LoadMachines(configDirectory);
        foreach (var shared in sharedConfigs)
        {
            result[shared.Machine.Id] = ShellSharedAdapter.ToMachineConfig(shared);
        }

        // 疊加 App 專屬的 Machine*.config.json
        if (Directory.Exists(configDirectory))
        {
            var files = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var config = JsonSerializer.Deserialize<MachineConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config?.Machine.Enable == true)
                    {
                        result[config.Machine.Id] = config;
                    }
                }
                catch
                {
                    // 略過無法解析的檔案
                }
            }
        }

        return [.. result.Values.Where(x => x.Machine.Enable)];
    }

    public static AppMeta LoadMeta(string filePath)
    {
        var sharedMeta = ShellSharedAdapter.ToAppMeta(ShellAppMetaLoader.Load(filePath));

        try
        {
            if (!File.Exists(filePath))
            {
                return sharedMeta;
            }

            var json = File.ReadAllText(filePath);
            var meta = JsonSerializer.Deserialize<AppMeta>(json, new JsonSerializerOptions
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

    private static AppMeta MergeMeta(AppMeta baseline, AppMeta overlay)
    {
        return new AppMeta
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
