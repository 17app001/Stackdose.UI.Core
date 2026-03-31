namespace Stackdose.App.MyDevice.Handlers;

/// <summary>
/// PLC 數據變動事件處理器
/// 觸發條件在各機器 Config/*.config.json → dataEvents 定義
/// 規則：值未變動不觸發；第一次掃描只記錄初始值，不觸發
/// </summary>
public sealed class DataEventHandlers
{
    public void HandleEvent(string eventName, string address, int oldVal, int newVal)
    {
        switch (eventName)
        {
            // Machine 1 events:
            case "OnEvent1": OnEvent1(address, newVal != 0, oldVal != 0); break;
            case "OnEvent2": OnEvent2(address, newVal, oldVal); break;
            case "OnEvent3": OnEvent3(address, newVal != 0, oldVal != 0); break;
            case "OnEvent4": OnEvent4(address, newVal, oldVal); break;
            case "OnEvent5": OnEvent5(address, newVal, oldVal); break;
        }
    }

    // ── Machine 1 (M1) ──

    /// <summary>M200 數值變動</summary>
    public void OnEvent1(string address, bool newVal, bool oldVal)
    {
        // TODO: 在此填寫邏輯
    }

    /// <summary>M201 數值變動</summary>
    public void OnEvent2(string address, int newVal, int oldVal)
    {
        // TODO: 在此填寫邏輯
    }

    /// <summary>M202 數值變動</summary>
    public void OnEvent3(string address, bool newVal, bool oldVal)
    {
        // TODO: 在此填寫邏輯
    }

    /// <summary>M203 數值變動</summary>
    public void OnEvent4(string address, int newVal, int oldVal)
    {
        // TODO: 在此填寫邏輯
    }

    /// <summary>M204 數值變動</summary>
    public void OnEvent5(string address, int newVal, int oldVal)
    {
        // TODO: 在此填寫邏輯
    }
}
