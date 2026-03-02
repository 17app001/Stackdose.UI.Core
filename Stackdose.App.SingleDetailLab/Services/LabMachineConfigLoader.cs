using Stackdose.App.SingleDetailLab.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.SingleDetailLab.Services;

internal static class LabMachineConfigLoader
{
    public static IReadOnlyList<LabMachineConfig> LoadEnabledMachines(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
        {
            return [];
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = new List<LabMachineConfig>();
        foreach (var path in Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<LabMachineConfig>(json, options);
                if (config is not null && config.Machine.Enable)
                {
                    result.Add(config);
                }
            }
            catch
            {
                // Skip invalid config for lab resilience.
            }
        }

        return result;
    }
}
