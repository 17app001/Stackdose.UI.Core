namespace Stackdose.App.UbiDemo.Models;

/// <summary>
/// Ubi 專用的裝置上下文 — 保留硬編碼屬性供 UbiDevicePage XAML binding 使用。
/// 由 UbiDeviceContextMapper.FromFrameworkContext 從框架的 DeviceContext 轉換而來。
/// </summary>
public sealed class UbiDeviceContext
{
    public string MachineId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string BatchAddress { get; set; } = "--";
    public string RecipeAddress { get; set; } = "--";
    public string NozzleAddress { get; set; } = "--";
    public string RunningAddress { get; set; } = "--";
    public string CompletedAddress { get; set; } = "--";
    public string AlarmAddress { get; set; } = "--";
    public string StartCommandAddress { get; set; } = "--";
    public string PauseCommandAddress { get; set; } = "--";
    public string StopCommandAddress { get; set; } = "--";
    public string AlarmConfigFile { get; set; } = string.Empty;
    public string SensorConfigFile { get; set; } = string.Empty;
    public string PrintHead1ConfigFile { get; set; } = string.Empty;
    public string PrintHead2ConfigFile { get; set; } = string.Empty;
    public string TotalTrayAddress { get; set; } = "D3400";
    public string CurrentTrayAddress { get; set; } = "D33";
    public string TotalLayerAddress { get; set; } = "D3401";
    public string CurrentLayerAddress { get; set; } = "D32";
    public string SwitchGraphicLayerAddress { get; set; } = "D510";
    public string SwitchAreaLayerAddress { get; set; } = "D512";
    public string MessageIdAddress { get; set; } = "D85";
    public string BatteryAddress { get; set; } = "D120";
    public string ElapsedTimeAddress { get; set; } = "D86";
    public string PrintHeadCountAddress { get; set; } = "D87";
}
