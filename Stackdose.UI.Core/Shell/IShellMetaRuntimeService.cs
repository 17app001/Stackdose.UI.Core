namespace Stackdose.UI.Core.Shell;

public interface IShellMetaRuntimeService<TMeta, TSnapshot> : IDisposable
    where TMeta : IShellAppProfile
    where TSnapshot : IShellMetaSnapshot
{
    event EventHandler<ShellMetaSnapshotChangedEventArgs<TSnapshot>>? SnapshotChanged;

    TSnapshot CurrentSnapshot { get; }

    TSnapshot Start(string configDirectory, string metaFilePath, TMeta initialMeta, bool enableMetaHotReload);

    void Stop();
}

public sealed class ShellMetaSnapshotChangedEventArgs<TSnapshot> : EventArgs
    where TSnapshot : IShellMetaSnapshot
{
    public ShellMetaSnapshotChangedEventArgs(TSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public TSnapshot Snapshot { get; }
}
