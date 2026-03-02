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
            return new ShellRuntimeContext(
                overviewPage,
                new Dictionary<string, ShellMachineConfig>(StringComparer.OrdinalIgnoreCase),
                meta);
        }

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.CurrentMachineDisplayName = configs[0].Machine.Name;
        shell.PageTitle = meta.DefaultPageTitle;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        return new ShellRuntimeContext(overviewPage, machines, meta);
    }

}

public sealed class ShellRuntimeContext
{
    public ShellRuntimeContext(
        MachineOverviewPage overviewPage,
        IReadOnlyDictionary<string, ShellMachineConfig> machines,
        ShellAppMeta meta)
    {
        OverviewPage = overviewPage;
        Machines = machines;
        Meta = meta;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, ShellMachineConfig> Machines { get; }
    public ShellAppMeta Meta { get; }

    public string GetAlarmConfigFile(string machineId)
    {
        if (Machines.TryGetValue(machineId, out var machine)
            && !string.IsNullOrWhiteSpace(machine.AlarmConfigFile))
        {
            return machine.AlarmConfigFile;
        }

        return string.Empty;
    }

    public string GetSensorConfigFile(string machineId)
    {
        if (Machines.TryGetValue(machineId, out var machine)
            && !string.IsNullOrWhiteSpace(machine.SensorConfigFile))
        {
            return machine.SensorConfigFile;
        }

        return string.Empty;
    }
}
