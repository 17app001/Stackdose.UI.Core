namespace Stackdose.App.MyDevice.Handlers;

/// <summary>
/// 命令處理器 — 回傳 true = 框架寫 PLC，回傳 false = 跳過。
/// </summary>
public sealed class CommandHandlers
{
    public bool HandleCommand(string machineId, string commandName, string address)
    {
        return (machineId, commandName) switch
        {
            ("M1", "Start1") => OnM1Start1(machineId, address),
            ("M1", "Start2") => OnM1Start2(machineId, address),
            ("M1", "Start3") => OnM1Start3(machineId, address),
            ("M1", "Start4") => OnM1Start4(machineId, address),
            ("M1", "Start5") => OnM1Start5(machineId, address),
            _ => true,
        };
    }

    // ── Machine 1 (M1) ──

    /// <summary>Start1 (M300)</summary>
    public bool OnM1Start1(string machineId, string address)
    {
        // TODO: 填入 Start1 邏輯
        return true;
    }

    /// <summary>Start2 (M301)</summary>
    public bool OnM1Start2(string machineId, string address)
    {
        // TODO: 填入 Start2 邏輯
        return true;
    }

    /// <summary>Start3 (M302)</summary>
    public bool OnM1Start3(string machineId, string address)
    {
        // TODO: 填入 Start3 邏輯
        return true;
    }

    /// <summary>Start4 (M303)</summary>
    public bool OnM1Start4(string machineId, string address)
    {
        // TODO: 填入 Start4 邏輯
        return true;
    }

    /// <summary>Start5 (M304)</summary>
    public bool OnM1Start5(string machineId, string address)
    {
        // TODO: 填入 Start5 邏輯
        return true;
    }
}
