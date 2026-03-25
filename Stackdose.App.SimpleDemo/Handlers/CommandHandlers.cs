namespace Stackdose.App.SimpleDemo.Handlers;

/// <summary>
/// 命令處理器 — 回傳 true = 框架寫 PLC，回傳 false = 跳過。
/// </summary>
public sealed class CommandHandlers
{
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
            _ => true,
        };
    }

    // ── Conveyor Line A (CVR1) ──

    /// <summary>Start (M300)</summary>
    public bool OnCVR1Start(string machineId, string address)
    {
        // TODO: 填入 Start 邏輯
        return true;
    }

    /// <summary>Stop (M301)</summary>
    public bool OnCVR1Stop(string machineId, string address)
    {
        // TODO: 填入 Stop 邏輯
        return true;
    }

    /// <summary>EmergencyStop (M302)</summary>
    public bool OnCVR1EmergencyStop(string machineId, string address)
    {
        // TODO: 填入 EmergencyStop 邏輯
        return true;
    }

    /// <summary>Reset (M303)</summary>
    public bool OnCVR1Reset(string machineId, string address)
    {
        // TODO: 填入 Reset 邏輯
        return true;
    }

    // ── Dispenser Unit B (DSP1) ──

    /// <summary>Start (M400)</summary>
    public bool OnDSP1Start(string machineId, string address)
    {
        // TODO: 填入 Start 邏輯
        return true;
    }

    /// <summary>Pause (M401)</summary>
    public bool OnDSP1Pause(string machineId, string address)
    {
        // TODO: 填入 Pause 邏輯
        return true;
    }

    /// <summary>Stop (M402)</summary>
    public bool OnDSP1Stop(string machineId, string address)
    {
        // TODO: 填入 Stop 邏輯
        return true;
    }

    /// <summary>Prime (M403)</summary>
    public bool OnDSP1Prime(string machineId, string address)
    {
        // TODO: 填入 Prime 邏輯
        return true;
    }

    /// <summary>Purge (M404)</summary>
    public bool OnDSP1Purge(string machineId, string address)
    {
        // TODO: 填入 Purge 邏輯
        return true;
    }
}
