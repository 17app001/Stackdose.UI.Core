namespace Stackdose.App.MyOvenDemo.Handlers;

/// <summary>維護模式處理器 — 回傳 true = 允許操作。</summary>
public sealed class MaintenanceHandlers
{

    // ── Reflow Oven A (OVEN1) ──

    /// <summary>�[���� ON/OFF (M600, toggle)</summary>
    public bool OnOVEN1HeaterOn(string machineId, string address)
    {
        // TODO: 填入 �[���� ON/OFF 的安全檢查邏輯
        return true;
    }

    /// <summary>������t (D700, editor)</summary>
    public bool OnOVEN1FanSpeed(string machineId, string address)
    {
        // TODO: 填入 ������t 的安全檢查邏輯
        return true;
    }

    /// <summary>��e�a�o�� (M601, momentary)</summary>
    public bool OnOVEN1ConveyorJog(string machineId, string address)
    {
        // TODO: 填入 ��e�a�o�� 的安全檢查邏輯
        return true;
    }

    /// <summary>�l�źʱ� (D100, readonly)</summary>
    public bool OnOVEN1OvenTemp(string machineId, string address)
    {
        // TODO: 填入 �l�źʱ� 的安全檢查邏輯
        return true;
    }

    // ── Reflow Oven B (OVEN2) ──

    /// <summary>�[���� ON/OFF (M700, toggle)</summary>
    public bool OnOVEN2HeaterOn(string machineId, string address)
    {
        // TODO: 填入 �[���� ON/OFF 的安全檢查邏輯
        return true;
    }

    /// <summary>������t (D800, editor)</summary>
    public bool OnOVEN2FanSpeed(string machineId, string address)
    {
        // TODO: 填入 ������t 的安全檢查邏輯
        return true;
    }

    /// <summary>��e�a�o�� (M701, momentary)</summary>
    public bool OnOVEN2ConveyorJog(string machineId, string address)
    {
        // TODO: 填入 ��e�a�o�� 的安全檢查邏輯
        return true;
    }
}
