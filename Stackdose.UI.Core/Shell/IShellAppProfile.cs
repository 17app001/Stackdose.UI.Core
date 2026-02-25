namespace Stackdose.UI.Core.Shell;

public interface IShellAppProfile
{
    string AppId { get; }

    string HeaderDeviceName { get; }

    string DefaultPageTitle { get; }

    bool UseFrameworkShellServices { get; }

    bool EnableMetaHotReload { get; }

    IReadOnlyList<ShellNavigationProfileItem> NavigationItems { get; }
}

public sealed record ShellNavigationProfileItem(
    string Title,
    string NavigationTarget,
    string RequiredLevel = "Operator");
