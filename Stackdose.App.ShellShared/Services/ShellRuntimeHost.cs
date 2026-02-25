using Stackdose.App.ShellShared.Models;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.IO;

namespace Stackdose.App.ShellShared.Services;

public static class ShellRuntimeHost
{
    public static ShellRuntimeContext? Start(MainContainer shell)
    {
        if (shell.ShellContent is not MachineOverviewPage overviewPage)
        {
            return null;
        }

        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var meta = ShellAppMetaLoader.Load(Path.Combine(configDir, "app-meta.json"));
        ShellOverviewBinder.ApplyMeta(overviewPage, meta);

        var configs = ShellConfigLoader.LoadMachines(configDir);
        ShellOverviewBinder.Bind(overviewPage, configs);

        if (configs.Count == 0)
        {
            shell.CurrentMachineDisplayName = string.Empty;
            shell.PageTitle = meta.DefaultPageTitle;
            shell.HeaderDeviceName = meta.HeaderDeviceName;
            return new ShellRuntimeContext(overviewPage, new Dictionary<string, ShellMachineConfig>(StringComparer.OrdinalIgnoreCase));
        }

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.CurrentMachineDisplayName = configs[0].Machine.Name;
        shell.PageTitle = meta.DefaultPageTitle;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        return new ShellRuntimeContext(overviewPage, machines);
    }

    public static string GetAlarmConfigFile(string machineId)
    {
        return string.Equals(machineId, "M1", StringComparison.OrdinalIgnoreCase)
            ? "Config/MachineA.alarms.json"
            : string.Equals(machineId, "M2", StringComparison.OrdinalIgnoreCase)
                ? "Config/MachineB.alarms.json"
                : string.Empty;
    }

    public static string GetSensorConfigFile(string machineId)
    {
        return string.Equals(machineId, "M1", StringComparison.OrdinalIgnoreCase)
            ? "Config/MachineA.sensors.json"
            : string.Equals(machineId, "M2", StringComparison.OrdinalIgnoreCase)
                ? "Config/MachineB.sensors.json"
                : string.Empty;
    }
}

public sealed class ShellRuntimeContext
{
    public ShellRuntimeContext(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, ShellMachineConfig> machines)
    {
        OverviewPage = overviewPage;
        Machines = machines;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, ShellMachineConfig> Machines { get; }
}
