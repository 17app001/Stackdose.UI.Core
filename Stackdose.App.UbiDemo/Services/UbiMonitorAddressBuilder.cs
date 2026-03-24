using Stackdose.App.ShellShared.Services;
using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Stackdose.App.UbiDemo.Services;

internal static class UbiMonitorAddressBuilder
{
    private static readonly Regex AddressPattern = new("^([A-Za-z]+)(\\d+)$", RegexOptions.Compiled);

    public static string Build(IEnumerable<UbiMachineConfig> configs, IUbiRuntimeMappingAdapter adapter)
    {
        var configList = configs as IReadOnlyList<UbiMachineConfig> ?? configs.ToList();
        var sharedAddresses = ShellMonitorAddressBuilder.CollectReadableAddresses(
            configList.Select(UbiShellSharedAdapter.ToShellMachineConfig));

        // ?? 新增：預先將所有詳細頁面的 Sensor / PlcEvent / PlcLabel 等位址也納入，
        // 確保在一開始連線就全註冊，避免切換頁面時重複堆疊註冊
        var preRegistrationAddresses = CollectAllComponentAddresses(configList, adapter);

        var addresses = sharedAddresses
            .Concat(adapter.GetManualPlcMonitorAddresses(configList))
            .Concat(adapter.GetDetailLabelAddresses(configList))
            .Concat(adapter.GetMachineAlertAddresses(configList))
            .Concat(preRegistrationAddresses) // 加入全部預先註冊的位址
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var parsed = addresses
            .Select(ParseAddress)
            .Where(x => x != null)
            .Cast<(string Prefix, int Number)>()
            .OrderBy(x => x.Prefix)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < parsed.Count)
        {
            var start = parsed[i];
            var end = i;

            while (end + 1 < parsed.Count
                   && parsed[end + 1].Prefix == start.Prefix
                   && parsed[end + 1].Number == parsed[end].Number + 1)
            {
                end++;
            }

            var length = end - i + 1;
            groups.Add(length > 1 ? $"{start.Prefix}{start.Number},{length}" : $"{start.Prefix}{start.Number}");
            i = end + 1;
        }

        return string.Join(",", groups);
    }

    public static MachineOverviewCard CreateCard(UbiMachineConfig config) => new()
    {
        MachineId = config.Machine.Id,
        Title = config.Machine.Name,
        BatchValue = "--",
        RecipeText = "--",
        StatusText = "Idle",
        StatusBrush = Brushes.SlateGray,
        LeftTopLabel = "Heartbeat",
        LeftTopValue = "0",
        LeftBottomLabel = "Alarm",
        LeftBottomValue = "0",
        RightTopLabel = "Nozzle",
        RightTopValue = "--",
        RightBottomLabel = "Mode",
        RightBottomValue = "Manual"
    };

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = AddressPattern.Match(address ?? string.Empty);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }

    // ?? 新增方法：遍歷所有機器的設定，蒐集可能會用到的位址
    private static IEnumerable<string> CollectAllComponentAddresses(IReadOnlyList<UbiMachineConfig> configs, IUbiRuntimeMappingAdapter adapter)
    {
        var addresses = new List<string>();
        foreach (var config in configs)
        {
            // Sensor 位址可以從 Config 取得
            var sensorConfigPath = adapter.GetSensorConfigFile(config);
            if (!string.IsNullOrWhiteSpace(sensorConfigPath) && System.IO.File.Exists(sensorConfigPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(sensorConfigPath, System.Text.Encoding.UTF8);
                    
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            if (item.TryGetProperty("Device", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var device = d.GetString();
                                if (!string.IsNullOrWhiteSpace(device))
                                {
                                    addresses.Add(device);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // Event 位址 可以從 Alarm Config 取得
            var eventConfigPath = adapter.GetAlarmConfigFile(config);
            if (!string.IsNullOrWhiteSpace(eventConfigPath) && System.IO.File.Exists(eventConfigPath))
            {
                 try
                {
                    string json = System.IO.File.ReadAllText(eventConfigPath, System.Text.Encoding.UTF8);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Alarms", out var alarms) && alarms.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in alarms.EnumerateArray())
                        {
                            if (item.TryGetProperty("Device", out var d) && d.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var device = d.GetString();
                                if (!string.IsNullOrWhiteSpace(device))
                                {
                                    addresses.Add(device);
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // 把 "M10,1" 或 "D20,5" 這樣的格式還原成純位址，這裡我們只要找出基礎位址，
        // 底下的 ParseAddress 會把它處理好
        return addresses
            .Select(a => {
                var parts = a.Split(',');
                return parts.Length > 0 ? parts[0].Trim() : string.Empty;
            })
            .Where(a => !string.IsNullOrEmpty(a));
    }
}
