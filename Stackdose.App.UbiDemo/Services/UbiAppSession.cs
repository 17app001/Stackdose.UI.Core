using Stackdose.UI.Core.Shell;
using System.Collections;

namespace Stackdose.App.UbiDemo.Services;

internal sealed class UbiAppSession
{
    public UbiAppSession(
        UbiMainWindowBootstrapState bootstrapState,
        UbiNavigationOrchestrator navigationOrchestrator)
    {
        Runtime = bootstrapState.Runtime;
        Shell = bootstrapState.Shell;
        ServiceMode = bootstrapState.ServiceMode;
        MachineOptions = bootstrapState.MachineOptions;
        CurrentMetaSnapshot = bootstrapState.InitialMetaSnapshot;
        NavigationOrchestrator = navigationOrchestrator;
    }

    public UbiRuntimeContext Runtime { get; }

    public UbiShellCoordinator Shell { get; }

    public ShellServiceMode ServiceMode { get; }

    public IEnumerable MachineOptions { get; }

    public UbiMetaSnapshot CurrentMetaSnapshot { get; set; }

    public UbiNavigationOrchestrator NavigationOrchestrator { get; }
}
