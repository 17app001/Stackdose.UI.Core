namespace Stackdose.App.DesignPlayer.Models;

/// <summary>
/// app-config.json 的資料模型，控制 App 標題、PLC 連線、設計稿路徑。
/// </summary>
public sealed class PlayerAppConfig
{
    /// <summary>視窗與 Header 顯示的 App 名稱</summary>
    public string AppTitle { get; set; } = "Stackdose Monitor";

    /// <summary>Header 右上角的設備型號標籤</summary>
    public string HeaderDeviceName { get; set; } = "MONITOR";

    /// <summary>啟動時是否顯示 LoginDialog（false = 直接以 Guest 身份進入）</summary>
    public bool LoginRequired { get; set; } = false;

    /// <summary>PLC 連線設定</summary>
    public PlcConnectionConfig Plc { get; set; } = new();

    /// <summary>.machinedesign.json 路徑（相對於執行目錄或絕對路徑）</summary>
    public string DesignFile { get; set; } = "Config/monitor.machinedesign.json";
}

public sealed class PlcConnectionConfig
{
    public string Ip            { get; set; } = "192.168.1.100";
    public int    Port          { get; set; } = 3000;
    public int    PollIntervalMs { get; set; } = 500;
    public bool   AutoConnect   { get; set; } = true;
}
