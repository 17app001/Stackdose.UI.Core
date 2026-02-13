using Stackdose.App.Demo.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.Demo.Services;

public static class DemoConfigLoader
{
    public static List<DemoMachineConfig> LoadMachines(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly);
        var result = new List<DemoMachineConfig>();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var config = JsonSerializer.Deserialize<DemoMachineConfig>(json, new JsonSerializerOptions
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
