using Stackdose.App.ShellShared.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.ShellShared.Services;

public static class ShellConfigLoader
{
    public static List<ShellMachineConfig> LoadMachines(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly);
        var result = new List<ShellMachineConfig>();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<ShellMachineConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config?.Machine.Enable == true)
                {
                    result.Add(config);
                }
            }
            catch
            {
            }
        }

        return result;
    }
}
