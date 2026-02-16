using Stackdose.App.Demo.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.Demo.Services;

public static class DemoAppMetaLoader
{
    public static DemoAppMeta Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new DemoAppMeta();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var meta = JsonSerializer.Deserialize<DemoAppMeta>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return meta ?? new DemoAppMeta();
        }
        catch
        {
            return new DemoAppMeta();
        }
    }
}
