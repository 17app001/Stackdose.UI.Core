using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.IO;

namespace Stackdose.App.Demo.Services;

public static class DemoRuntimeHost
{
    public static DemoRuntimeContext? Start(MainContainer shell)
    {
        if (shell.ShellContent is not MachineOverviewPage overviewPage)
        {
            return null;
        }

        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var meta = DemoAppMetaLoader.Load(Path.Combine(configDir, "app-meta.json"));
        DemoOverviewBinder.ApplyMeta(overviewPage, meta);

        var configs = DemoConfigLoader.LoadMachines(configDir);
        DemoOverviewBinder.Bind(overviewPage, configs);

        if (configs.Count == 0)
        {
            shell.CurrentMachineDisplayName = string.Empty;
            shell.PageTitle = meta.DefaultPageTitle;
            shell.HeaderDeviceName = meta.HeaderDeviceName;
            return new DemoRuntimeContext(overviewPage, new Dictionary<string, Models.DemoMachineConfig>(StringComparer.OrdinalIgnoreCase));
        }

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.CurrentMachineDisplayName = configs[0].Machine.Name;
        shell.PageTitle = meta.DefaultPageTitle;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        return new DemoRuntimeContext(overviewPage, machines);
    }
}

public sealed class DemoRuntimeContext
{
    public DemoRuntimeContext(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, Models.DemoMachineConfig> machines)
    {
        OverviewPage = overviewPage;
        Machines = machines;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, Models.DemoMachineConfig> Machines { get; }
}
