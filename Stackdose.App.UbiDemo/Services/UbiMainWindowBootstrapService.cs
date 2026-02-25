using Stackdose.App.UbiDemo.Models;
using Stackdose.UI.Core.Shell;
using Stackdose.UI.Templates.Shell;
using System.Collections;
using System.Windows.Input;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiMainWindowBootstrapService
{
    public UbiMainWindowBootstrapState? Start(
        MainContainer mainShell,
        UbiMetaRuntimeService metaRuntimeService,
        IShellNavigationService navigationService,
        ICommand navigationCommand,
        ICommand machineSelectionCommand,
        Action showOverview,
        Action showCurrentOrFirstMachineDetail,
        Action showLogViewer,
        Action showUserManagement,
        Action showSettings)
    {
        var runtime = UbiRuntimeHost.Start(mainShell);
        if (runtime is null)
        {
            return null;
        }

        var mode = runtime.AppMeta.UseFrameworkShellServices
            ? ShellServiceMode.Framework
            : ShellServiceMode.Legacy;

        var initialMetaSnapshot = metaRuntimeService.Start(
            runtime.ConfigDirectory,
            runtime.MetaFilePath,
            runtime.AppMeta,
            runtime.AppMeta.EnableMetaHotReload);

        var shell = CreateCoordinator(mode, mainShell);
        var shellPages = CreateShellPageService(mode, shell, navigationService, mainShell);

        mainShell.NavigationCommand = navigationCommand;
        mainShell.MachineSelectionCommand = machineSelectionCommand;

        navigationService.RegisterDefaultHandlers(
            showOverview,
            showCurrentOrFirstMachineDetail,
            showLogViewer,
            showUserManagement,
            showSettings);

        UbiRuntimeMapper.BuildRuntimeMaps(runtime.Machines);
        shell.SelectNavigation(ShellNavigationTargets.Overview);

        return new UbiMainWindowBootstrapState(
            runtime,
            shell,
            shellPages,
            initialMetaSnapshot,
            mode,
            BuildMachineOptions(runtime.Machines));
    }

    public void Stop(
        MainContainer mainShell,
        UbiMetaRuntimeService metaRuntimeService,
        IShellNavigationService navigationService,
        UbiDevicePageService devicePages)
    {
        mainShell.NavigationCommand = null;
        mainShell.MachineSelectionCommand = null;
        metaRuntimeService.Stop();
        navigationService.Clear();
        devicePages.Clear();
    }

    private static List<KeyValuePair<string, string>> BuildMachineOptions(IReadOnlyDictionary<string, UbiMachineConfig> machines)
    {
        return machines.Values
            .Select(machine => new KeyValuePair<string, string>(
                machine.Machine.Id,
                $"{machine.Machine.Name} ({machine.Machine.Id})"))
            .ToList();
    }

    private static UbiShellCoordinator CreateCoordinator(ShellServiceMode mode, MainContainer mainShell)
    {
        return mode switch
        {
            ShellServiceMode.Framework => new UbiShellCoordinator(mainShell, mainShell.PageTitle),
            _ => new UbiShellCoordinator(mainShell, mainShell.PageTitle)
        };
    }

    private static UbiShellPageService CreateShellPageService(
        ShellServiceMode mode,
        UbiShellCoordinator shell,
        IShellNavigationService navigationService,
        MainContainer mainShell)
    {
        return mode switch
        {
            ShellServiceMode.Framework => new UbiShellPageService(shell, navigationService, mainShell),
            _ => new UbiShellPageService(shell, navigationService, mainShell)
        };
    }
}

internal sealed class UbiMainWindowBootstrapState
{
    public UbiMainWindowBootstrapState(
        UbiRuntimeContext runtime,
        UbiShellCoordinator shell,
        UbiShellPageService shellPages,
        UbiMetaSnapshot initialMetaSnapshot,
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

    public UbiRuntimeContext Runtime { get; }
    public UbiShellCoordinator Shell { get; }
    public UbiShellPageService ShellPages { get; }
    public UbiMetaSnapshot InitialMetaSnapshot { get; }
    public ShellServiceMode ServiceMode { get; }
    public IEnumerable MachineOptions { get; }
}
