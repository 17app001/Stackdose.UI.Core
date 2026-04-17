namespace Stackdose.Tools.MachinePageDesigner.Models;

/// <summary>
/// app-config.json 的編輯草稿（設計器內用，不依賴 DesignPlayer 專案）。
/// 序列化結構與 PlayerAppConfig 完全相同。
/// </summary>
public sealed class AppConfigDraft
{
    // ── App ──────────────────────────────────────────────────────
    public string AppTitle         { get; set; } = "Stackdose Monitor";
    public string HeaderDeviceName { get; set; } = "MONITOR";
    public bool   LoginRequired    { get; set; } = false;

    // ── PLC ──────────────────────────────────────────────────────
    public string PlcIp           { get; set; } = "192.168.1.100";
    public int    PlcPort         { get; set; } = 3000;
    public int    PollIntervalMs  { get; set; } = 500;
    public bool   AutoConnect     { get; set; } = true;

    // ── Design ───────────────────────────────────────────────────
    /// <summary>
    /// 相對於 DesignPlayer.exe 的設計稿路徑，慣例為 "Config/{filename}.machinedesign.json"。
    /// </summary>
    public string DesignFile { get; set; } = "";
}
