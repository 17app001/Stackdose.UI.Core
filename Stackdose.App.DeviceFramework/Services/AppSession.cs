using Stackdose.UI.Core.Shell;
using System.Collections;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 通用 App Session — 儲存一次 Bootstrap 的所有狀態。
/// </summary>
public sealed class AppSession
{
    public AppSession(
        BootstrapState bootstrapState,
        NavigationOrchestrator navigationOrchestrator)
    {
        Runtime = bootstrapState.Runtime;
        Shell = bootstrapState.Shell;
        ServiceMode = bootstrapState.ServiceMode;
        MachineOptions = bootstrapState.MachineOptions;
        CurrentMetaSnapshot = bootstrapState.InitialMetaSnapshot;
        NavigationOrchestrator = navigationOrchestrator;
    }

    public RuntimeContext Runtime { get; }
    public ShellCoordinator Shell { get; }
    public ShellServiceMode ServiceMode { get; }
    public IEnumerable MachineOptions { get; }
    public MetaSnapshot CurrentMetaSnapshot { get; set; }
    public NavigationOrchestrator NavigationOrchestrator { get; }
}
