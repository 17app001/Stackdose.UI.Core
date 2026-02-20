using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stackdose.App.UbiDemo.Services;

public static class UbiRuntimeHost
{
    public static UbiRuntimeContext? Start(MainContainer shell)
    {
        if (shell.ShellContent is not MachineOverviewPage overviewPage)
        {
            return null;
        }

        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var meta = UbiRuntimeLoader.LoadMeta(Path.Combine(configDir, "app-meta.json"));
        UbiRuntimeMapper.ApplyMeta(overviewPage, meta);

        var configs = UbiRuntimeLoader.LoadMachines(configDir);
        UbiRuntimeMapper.BindOverview(overviewPage, configs);

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.PageTitle = meta.DefaultPageTitle;
        shell.CurrentMachineDisplayName = configs.Count > 0 ? configs[0].Machine.Name : string.Empty;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        return new UbiRuntimeContext(overviewPage, machines);
    }
}

public sealed class UbiRuntimeContext
{
    public UbiRuntimeContext(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        OverviewPage = overviewPage;
        Machines = machines;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, UbiMachineConfig> Machines { get; }
}
