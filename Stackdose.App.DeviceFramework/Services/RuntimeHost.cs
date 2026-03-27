using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.IO;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// �q�� RuntimeHost �X ���J Config�B��l�� Overview �����C
/// �i�z�L�غc�l�`�J�ۭq RuntimeMapper�C
/// </summary>
public class RuntimeHost
{
    private readonly RuntimeMapper _runtimeMapper;
    private readonly string? _projectFolderName;

    public RuntimeHost(RuntimeMapper? runtimeMapper = null, string? projectFolderName = null)
    {
        _runtimeMapper = runtimeMapper ?? new RuntimeMapper();
        _projectFolderName = projectFolderName;
    }

    public RuntimeMapper Mapper => _runtimeMapper;

    /// <summary>
    /// Loads machine configs without requiring a shell. Use for SinglePage scenarios.
    /// </summary>
    public (List<MachineConfig> Configs, string ConfigDirectory) LoadConfigs()
    {
        var configDir = ResolveConfigDirectory();
        return (ConfigLoader.LoadMachines(configDir), configDir);
    }

    public RuntimeContext? Start(MainContainer shell)
    {
        if (shell.ShellContent is not MachineOverviewPage overviewPage)
            return null;

        var configDir = ResolveConfigDirectory();
        var meta = ConfigLoader.LoadMeta(Path.Combine(configDir, "app-meta.json"));
        _runtimeMapper.ApplyMeta(overviewPage, meta);

        var configs = ConfigLoader.LoadMachines(configDir);
        _runtimeMapper.BindOverview(overviewPage, configs);

        shell.HeaderDeviceName = meta.HeaderDeviceName;
        shell.PageTitle = meta.DefaultPageTitle;
        shell.CurrentMachineDisplayName = configs.Count > 0 ? configs[0].Machine.Name : string.Empty;

        var machines = configs.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        var metaFilePath = Path.Combine(configDir, "app-meta.json");
        return new RuntimeContext(overviewPage, machines, meta, configDir, metaFilePath);
    }

    private string ResolveConfigDirectory()
    {
        var candidates = new List<string>();

        var baseConfig = Path.Combine(AppContext.BaseDirectory, "Config");
        if (Directory.Exists(baseConfig))
            candidates.Add(baseConfig);

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            if (!string.IsNullOrWhiteSpace(_projectFolderName))
            {
                var projectConfig = Path.Combine(current.FullName, _projectFolderName, "Config");
                if (Directory.Exists(projectConfig))
                    candidates.Add(projectConfig);
            }

            var plainConfig = Path.Combine(current.FullName, "Config");
            if (Directory.Exists(plainConfig))
                candidates.Add(plainConfig);

            current = current.Parent;
        }

        var best = candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => new { Path = path, MetaFile = Path.Combine(path, "app-meta.json") })
            .Where(x => File.Exists(x.MetaFile))
            .Select(x => new { x.Path, LastWriteUtc = File.GetLastWriteTimeUtc(x.MetaFile) })
            .OrderByDescending(x => x.LastWriteUtc)
            .FirstOrDefault();

        return best?.Path ?? baseConfig;
    }
}

/// <summary>
/// Runtime �W�U�� �X ���J�᪺��Ū�ַӡC
/// </summary>
public sealed class RuntimeContext
{
    public RuntimeContext(
        MachineOverviewPage overviewPage,
        IReadOnlyDictionary<string, MachineConfig> machines,
        AppMeta appMeta,
        string configDirectory,
        string metaFilePath)
    {
        OverviewPage = overviewPage;
        Machines = machines;
        AppMeta = appMeta;
        ConfigDirectory = configDirectory;
        MetaFilePath = metaFilePath;
    }

    public MachineOverviewPage OverviewPage { get; }
    public IReadOnlyDictionary<string, MachineConfig> Machines { get; }
    public AppMeta AppMeta { get; }
    public string ConfigDirectory { get; }
    public string MetaFilePath { get; }
}
