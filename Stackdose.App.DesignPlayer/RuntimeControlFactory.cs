using Stackdose.Tools.MachinePageDesigner;

namespace Stackdose.App.DesignPlayer;

/// <summary>
/// DesignPlayer 專用的 Runtime 控制項工廠。
/// 繼承 <see cref="BaseRuntimeControlFactory"/>，設定 ContextName 為 "Player"。
/// </summary>
public sealed class RuntimeControlFactory : BaseRuntimeControlFactory
{
    public static readonly RuntimeControlFactory Instance = new();

    protected override string ContextName => "Player";
}

