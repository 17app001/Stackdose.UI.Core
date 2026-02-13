using Stackdose.App.PlcDevice.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.PlcDevice.Services;

public static class PlcMachineConfigLoader
{
    public static PlcMachineConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Config file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<PlcMachineConfig>(json, options);
        if (config == null)
        {
            throw new InvalidOperationException("Unable to deserialize PLC machine config.");
        }

        return config;
    }
}
