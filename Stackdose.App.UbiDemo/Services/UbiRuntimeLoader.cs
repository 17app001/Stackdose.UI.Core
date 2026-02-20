using Stackdose.App.UbiDemo.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeLoader
{
    public static List<UbiMachineConfig> LoadMachines(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(configDirectory, "Machine*.config.json", SearchOption.TopDirectoryOnly);
        var result = new List<UbiMachineConfig>();

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
                    result.Add(config);
                }
            }
            catch
            {
            }
        }

        return result;
    }

    public static UbiAppMeta LoadMeta(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new UbiAppMeta();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var meta = JsonSerializer.Deserialize<UbiAppMeta>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return meta ?? new UbiAppMeta();
        }
        catch
        {
            return new UbiAppMeta();
        }
    }
}
