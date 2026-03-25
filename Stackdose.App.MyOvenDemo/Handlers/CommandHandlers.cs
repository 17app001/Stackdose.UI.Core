namespace Stackdose.App.MyOvenDemo.Handlers;

/// <summary>
/// 命令處理器 — 回傳 true = 框架寫 PLC，回傳 false = 跳過。
/// </summary>
public sealed class CommandHandlers
{
    public bool HandleCommand(string machineId, string commandName, string address)
    {
        return (machineId, commandName) switch
        {
            ("OVEN1", "Start") => OnOVEN1Start(machineId, address),
            ("OVEN1", "Stop") => OnOVEN1Stop(machineId, address),
            ("OVEN1", "EmergencyStop") => OnOVEN1EmergencyStop(machineId, address),
            ("OVEN1", "Reset") => OnOVEN1Reset(machineId, address),
            ("OVEN2", "Start") => OnOVEN2Start(machineId, address),
            ("OVEN2", "Stop") => OnOVEN2Stop(machineId, address),
            ("OVEN2", "EmergencyStop") => OnOVEN2EmergencyStop(machineId, address),
            ("OVEN2", "Reset") => OnOVEN2Reset(machineId, address),
            _ => true,
        };
    }

    // ── Reflow Oven A (OVEN1) ──

    /// <summary>Start (M400)</summary>
    public bool OnOVEN1Start(string machineId, string address)
    {
        // TODO: 填入 Start 邏輯
        return true;
    }

    /// <summary>Stop (M401)</summary>
    public bool OnOVEN1Stop(string machineId, string address)
    {
        // TODO: 填入 Stop 邏輯
        return true;
    }

    /// <summary>EmergencyStop (M402)</summary>
    public bool OnOVEN1EmergencyStop(string machineId, string address)
    {
        // TODO: 填入 EmergencyStop 邏輯
        return true;
    }

    /// <summary>Reset (M403)</summary>
    public bool OnOVEN1Reset(string machineId, string address)
    {
        // TODO: 填入 Reset 邏輯
        return true;
    }

    // ── Reflow Oven B (OVEN2) ──

    /// <summary>Start (M500)</summary>
    public bool OnOVEN2Start(string machineId, string address)
    {
        // TODO: 填入 Start 邏輯
        return true;
    }

    /// <summary>Stop (M501)</summary>
    public bool OnOVEN2Stop(string machineId, string address)
    {
        // TODO: 填入 Stop 邏輯
        return true;
    }

    /// <summary>EmergencyStop (M502)</summary>
    public bool OnOVEN2EmergencyStop(string machineId, string address)
    {
        // TODO: 填入 EmergencyStop 邏輯
        return true;
    }

    /// <summary>Reset (M503)</summary>
    public bool OnOVEN2Reset(string machineId, string address)
    {
        // TODO: 填入 Reset 邏輯
        return true;
    }
}
