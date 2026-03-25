namespace Stackdose.App.SimpleDemo.Handlers;

/// <summary>
/// 命令處理器 — 每個 Command 按鈕點擊時呼叫對應方法。
/// 開發人員在 TODO 處填入實際業務邏輯。
///
/// 回傳 true  → 框架執行預設 PLC 寫入
/// 回傳 false → 框架跳過 PLC 寫入（由你自己控制）
/// </summary>
public sealed class CommandHandlers
{
    /// <summary>
    /// 命令分派入口 — 框架自動呼叫，根據 machineId + commandName 分派到對應方法。
    /// </summary>
    public bool HandleCommand(string machineId, string commandName, string address)
    {
        return (machineId, commandName) switch
        {
            ("CVR1", "Start") => OnCVR1Start(machineId, address),
            ("CVR1", "Stop") => OnCVR1Stop(machineId, address),
            ("CVR1", "EmergencyStop") => OnCVR1EmergencyStop(machineId, address),
            ("CVR1", "Reset") => OnCVR1Reset(machineId, address),
            ("DSP1", "Start") => OnDSP1Start(machineId, address),
            ("DSP1", "Pause") => OnDSP1Pause(machineId, address),
            ("DSP1", "Stop") => OnDSP1Stop(machineId, address),
            ("DSP1", "Prime") => OnDSP1Prime(machineId, address),
            ("DSP1", "Purge") => OnDSP1Purge(machineId, address),
            _ => true, // 未定義的命令，使用預設行為
        };
    }

    // ═══════════════════════════════════════
    //  Machine: Conveyor Line A (CVR1)
    // ═══════════════════════════════════════

    /// <summary>CVR1 — Start (M300)</summary>
    public bool OnCVR1Start(string machineId, string address)
    {
        // TODO: 在此填入 Start 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>CVR1 — Stop (M301)</summary>
    public bool OnCVR1Stop(string machineId, string address)
    {
        // TODO: 在此填入 Stop 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>CVR1 — EmergencyStop (M302)</summary>
    public bool OnCVR1EmergencyStop(string machineId, string address)
    {
        // TODO: 在此填入 EmergencyStop 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>CVR1 — Reset (M303)</summary>
    public bool OnCVR1Reset(string machineId, string address)
    {
        // TODO: 在此填入 Reset 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    // ═══════════════════════════════════════
    //  Machine: Dispenser Unit B (DSP1)
    // ═══════════════════════════════════════

    /// <summary>DSP1 — Start (M400)</summary>
    public bool OnDSP1Start(string machineId, string address)
    {
        // TODO: 在此填入 Start 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>DSP1 — Pause (M401)</summary>
    public bool OnDSP1Pause(string machineId, string address)
    {
        // TODO: 在此填入 Pause 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>DSP1 — Stop (M402)</summary>
    public bool OnDSP1Stop(string machineId, string address)
    {
        // TODO: 在此填入 Stop 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>DSP1 — Prime (M403)</summary>
    public bool OnDSP1Prime(string machineId, string address)
    {
        // TODO: 在此填入 Prime 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }

    /// <summary>DSP1 — Purge (M404)</summary>
    public bool OnDSP1Purge(string machineId, string address)
    {
        // TODO: 在此填入 Purge 的前置檢查或自訂邏輯
        //       回傳 true  = 框架自動寫入 PLC
        //       回傳 false = 跳過 PLC 寫入，由你自行處理
        return true;
    }
}
