using Stackdose.App.ShellShared.Models;
using System.IO;
using System.Text.Json;

namespace Stackdose.App.ShellShared.Services;

public static class ShellAppMetaLoader
{
    public static ShellAppMeta Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new ShellAppMeta();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var meta = JsonSerializer.Deserialize<ShellAppMeta>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return meta ?? new ShellAppMeta();
        }
        catch
        {
            return new ShellAppMeta();
        }
    }
}
