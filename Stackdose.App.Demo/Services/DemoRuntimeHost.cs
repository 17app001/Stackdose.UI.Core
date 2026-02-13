using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.IO;

namespace Stackdose.App.Demo.Services;

public static class DemoRuntimeHost
{
    public static void Start(MainContainer shell)
    {
        if (shell.ShellContent is not MachineOverviewPage overviewPage)
        {
            return;
        }

        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var configs = DemoConfigLoader.LoadMachines(configDir);
        DemoOverviewBinder.Bind(overviewPage, configs);

        if (configs.Count == 0)
        {
            shell.CurrentMachineDisplayName = string.Empty;
            shell.PageTitle = "System Overview";
            return;
        }

        shell.CurrentMachineDisplayName = configs[0].Machine.Name;
        shell.PageTitle = "Machine Overview";
    }
}
