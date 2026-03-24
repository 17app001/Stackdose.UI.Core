using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Shell;
using System.Collections;
using System.Windows.Input;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 Bootstrap 服務 — 負責啟動 / 停止整個 App 生命週期。
/// </summary>
public sealed class BootstrapService
{
    private readonly RuntimeHost _runtimeHost;

    public BootstrapService(RuntimeHost runtimeHost)
    {
        _runtimeHost = runtimeHost;
    }

    public BootstrapState? Start(
        MainContainer mainShell,
        MetaRuntimeService metaRuntimeService,
        IShellNavigationService navigationService,
        ICommand navigationCommand,
        ICommand machineSelectionCommand,
        Action showOverview,
        Action showCurrentOrFirstMachineDetail,
        Action showLogViewer,
        Action showUserManagement,
        Action showSettings)
    {
        var runtime = _runtimeHost.Start(mainShell);
        if (runtime is null)
            return null;

        var mode = runtime.AppMeta.UseFrameworkShellServices
            ? ShellServiceMode.Framework
            : ShellServiceMode.Legacy;

        var initialMetaSnapshot = metaRuntimeService.Start(
            runtime.ConfigDirectory,
            runtime.MetaFilePath,
            runtime.AppMeta,
            runtime.AppMeta.EnableMetaHotReload);

        var shell = new ShellCoordinator(mainShell, mainShell.PageTitle);
        var shellPages = new ShellPageService(shell, navigationService, mainShell);

        mainShell.NavigationCommand = navigationCommand;
        mainShell.MachineSelectionCommand = machineSelectionCommand;

        navigationService.RegisterDefaultHandlers(
            showOverview,
            showCurrentOrFirstMachineDetail,
            showLogViewer,
            showUserManagement,
            showSettings);

        _runtimeHost.Mapper.BuildRuntimeMaps(runtime.Machines);
        shell.SelectNavigation(ShellNavigationTargets.Overview);

        return new BootstrapState(
            runtime,
            shell,
            shellPages,
            initialMetaSnapshot,
            mode,
            BuildMachineOptions(runtime.Machines));
    }

    public void Stop(
        MainContainer mainShell,
        MetaRuntimeService metaRuntimeService,
        IShellNavigationService navigationService,
        DevicePageService devicePages)
    {
        mainShell.NavigationCommand = null;
        mainShell.MachineSelectionCommand = null;
        metaRuntimeService.Stop();
        navigationService.Clear();
        devicePages.Clear();
    }

    private static List<KeyValuePair<string, string>> BuildMachineOptions(IReadOnlyDictionary<string, MachineConfig> machines)
    {
        return machines.Values
            .Select(machine => new KeyValuePair<string, string>(
                machine.Machine.Id,
                $"{machine.Machine.Name} ({machine.Machine.Id})"))
            .ToList();
    }
}

/// <summary>
/// Bootstrap 狀態 — 啟動後的快照。
/// </summary>
public sealed class BootstrapState : IShellBootstrapState<MetaSnapshot>
{
    public BootstrapState(
        RuntimeContext runtime,
        ShellCoordinator shell,
        ShellPageService shellPages,
        MetaSnapshot initialMetaSnapshot,
        ShellServiceMode serviceMode,
        IEnumerable machineOptions)
    {
        Runtime = runtime;
        Shell = shell;
        ShellPages = shellPages;
        InitialMetaSnapshot = initialMetaSnapshot;
        ServiceMode = serviceMode;
        MachineOptions = machineOptions;
    }

    public RuntimeContext Runtime { get; }
    public ShellCoordinator Shell { get; }
    public ShellPageService ShellPages { get; }
    public MetaSnapshot InitialMetaSnapshot { get; }
    public ShellServiceMode ServiceMode { get; }
    public IEnumerable MachineOptions { get; }
}
