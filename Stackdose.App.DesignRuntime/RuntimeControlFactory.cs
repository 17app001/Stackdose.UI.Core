using Stackdose.Tools.MachinePageDesigner;

namespace Stackdose.App.DesignRuntime;

/// <summary>
/// DesignRuntime 專用的 Runtime 控制項工廠。
/// 繼承 <see cref="BaseRuntimeControlFactory"/>，設定 ContextName 為 "Runtime"。
/// </summary>
public sealed class RuntimeControlFactory : BaseRuntimeControlFactory
{
    public static readonly RuntimeControlFactory Instance = new();

    protected override string ContextName => "Runtime";
}

