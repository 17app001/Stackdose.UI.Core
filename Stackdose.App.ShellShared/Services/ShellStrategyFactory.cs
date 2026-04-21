namespace Stackdose.App.ShellShared.Services;

/// <summary>
/// 根據 DesignDocument.ShellMode 字串選擇對應的 IShellStrategy。
/// </summary>
public static class ShellStrategyFactory
{
    /// <summary>
    /// 選擇 Shell 策略。未知的 mode 值一律退回 FreeCanvas。
    /// </summary>
    public static IShellStrategy Select(string? shellMode) => shellMode?.ToLowerInvariant() switch
    {
        "singlepage" => new SinglePageShellStrategy(),
        "standard"   => new StandardShellStrategy(),
        _            => new FreeCanvasShellStrategy(),
    };
}
