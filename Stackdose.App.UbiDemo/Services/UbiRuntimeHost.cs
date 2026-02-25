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

        var configDir = ResolveConfigDirectory();
        var meta = UbiRuntimeLoader.LoadMeta(Path.Combine(configDir, "app-meta.json"));
        UbiRuntimeMapper.ApplyMeta(overviewPage, meta);

        var configs = UbiRuntimeLoader.LoadMachines(configDir);
        UbiRuntimeMapper.BindOverview(overviewPage, configs);

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.PageTitle = meta.DefaultPageTitle;
        shell.CurrentMachineDisplayName = configs.Count > 0 ? configs[0].Machine.Name : string.Empty;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        return new UbiRuntimeContext(overviewPage, machines, meta);
    }

    private static string ResolveConfigDirectory()
    {
        var candidates = new List<string>();

        var baseConfig = Path.Combine(AppContext.BaseDirectory, "Config");
        if (Directory.Exists(baseConfig))
        {
            candidates.Add(baseConfig);
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            var projectConfig = Path.Combine(current.FullName, "Stackdose.App.UbiDemo", "Config");
            if (Directory.Exists(projectConfig))
            {
                candidates.Add(projectConfig);
            }

            var plainConfig = Path.Combine(current.FullName, "Config");
            if (Directory.Exists(plainConfig))
            {
                candidates.Add(plainConfig);
            }

            current = current.Parent;
        }

        var best = candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new
            {
                Path = path,
                MetaFile = Path.Combine(path, "app-meta.json")
            })
            .Where(x => File.Exists(x.MetaFile))
            .Select(x => new
            {
                x.Path,
                LastWriteUtc = File.GetLastWriteTimeUtc(x.MetaFile)
            })
            .OrderByDescending(x => x.LastWriteUtc)
            .FirstOrDefault();

        return best?.Path ?? baseConfig;
    }
}

public sealed class UbiRuntimeContext
{
    public UbiRuntimeContext(MachineOverviewPage overviewPage, IReadOnlyDictionary<string, UbiMachineConfig> machines, UbiAppMeta appMeta)
    {
        OverviewPage = overviewPage;
        Machines = machines;
        AppMeta = appMeta;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, UbiMachineConfig> Machines { get; }
    public UbiAppMeta AppMeta { get; }
}
