namespace Stackdose.UI.Core.Shell;

public interface IShellMetaSnapshot
{
    string HeaderDeviceName { get; }

    string DefaultPageTitle { get; }

    IReadOnlyDictionary<string, string> NavigationTitles { get; }
}
