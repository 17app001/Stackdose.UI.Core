using System.Collections;
using System.Windows.Input;

namespace Stackdose.UI.Core.Shell;

public interface IShellBootstrapService<TShellHost, TMetaRuntimeService, TNavigationService, TBootstrapState, TDevicePages>
    where TBootstrapState : class
{
    TBootstrapState? Start(
        TShellHost shellHost,
        TMetaRuntimeService metaRuntimeService,
        TNavigationService navigationService,
        ICommand navigationCommand,
        ICommand machineSelectionCommand,
        Action showOverview,
        Action showCurrentOrFirstMachineDetail,
        Action showLogViewer,
        Action showUserManagement,
        Action showSettings);

    void Stop(
        TShellHost shellHost,
        TMetaRuntimeService metaRuntimeService,
        TNavigationService navigationService,
        TDevicePages devicePages);
}

public interface IShellBootstrapState<TSnapshot>
    where TSnapshot : IShellMetaSnapshot
{
    ShellServiceMode ServiceMode { get; }

    TSnapshot InitialMetaSnapshot { get; }

    IEnumerable MachineOptions { get; }
}
