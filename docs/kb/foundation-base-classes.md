# 控件基類體系（B1 產出）

> B1 重構結果：所有 Plc* 控件已從各自實作改為繼承統一基類，消除重複訂閱邏輯。

---

## 繼承關係

```
UserControl
  └── CyberControlBase（主題 + WeakRef 生命週期）
        └── PlcControlBase（PlcContext 訂閱 + ValueChanged 事件）
              ├── PlcLabel
              ├── PlcText
              ├── PlcStatusIndicator
              ├── SensorViewer
              └── AlarmViewer

TabControl
  └── CyberTabControl（主題支援）

PlcStatus — 不繼承（連線單例，不是 consumer）
```

---

## CyberControlBase

**位置：** `Stackdose.UI.Core/Controls/Base/CyberControlBase.cs`

提供：
- `IThemeAware` 介面 + `ThemeManager.Register(WeakReference(this))` 自動登錄/登出
- `Loaded` / `Unloaded` lifecycle 勾子
- `OnThemeChanged(ResourceDictionary theme)` 虛擬方法供子類覆寫

```csharp
public class MyControl : CyberControlBase
{
    protected override void OnThemeChanged(ResourceDictionary theme)
    {
        // 更新自訂色彩
    }
}
```

---

## PlcControlBase

**位置：** `Stackdose.UI.Core/Controls/Base/PlcControlBase.cs`

提供：

| 成員 | 說明 |
|---|---|
| `Address` DP | PLC 地址（共用，供子類直接用） |
| `CurrentValue` | 最新掃描值（object?） |
| `ValueChanged` event | `EventHandler<PlcValueChangedEventArgs>`，每次 PLC 掃描值更新觸發 |
| `OnPlcConnected(IPlcManager)` | 子類覆寫，PLC 連線後的初始化 |
| `OnPlcDataUpdated(IPlcManager)` | 子類覆寫，每次 ScanUpdated 時呼叫 |
| `OnGlobalStatusChanged(PlcStatus?)` | 子類覆寫，全域 PLC 狀態切換 |
| `RaiseValueChanged(...)` | 子類呼叫，同時寫入 `CurrentValue` 並觸發事件 |

ValueChanged 事件同時經由 `PlcEventContext.PublishControlValueChanged()` 廣播至 BehaviorEngine。

---

## PlcEventContext（B2 升級後）

**位置：** `Stackdose.UI.Core/Helpers/PlcEventContext.cs`

B2 後的兩個能力：

```csharp
// 1. ControlValueChanged 靜態事件（BehaviorEngine 訂閱）
PlcEventContext.ControlValueChanged += (sender, args) => { ... };

// 2. 廣播（由 PlcControlBase 內部呼叫，不需手動呼叫）
PlcEventContext.PublishControlValueChanged(controlElement, args);
```

原有的 bit edge 觸發（`PlcEventTrigger` 用）仍保留不變。

---

## PlcValueChangedEventArgs

**位置：** `Stackdose.UI.Core/Helpers/PlcValueChangedEventArgs.cs`

```csharp
public sealed class PlcValueChangedEventArgs : EventArgs
{
    public string Address     { get; init; }
    public object? RawValue   { get; init; }
    public string DisplayText { get; init; }
}
```

---

## 新增 Plc* 控件（標準做法）

```csharp
public partial class MyPlcControl : PlcControlBase
{
    // 1. 只需覆寫資料相關的兩個 hook
    protected override void OnPlcConnected(IPlcManager mgr)
    {
        mgr.Monitor?.Register(Address, 1);
    }

    protected override void OnPlcDataUpdated(IPlcManager mgr)
    {
        var raw = mgr.GetWord(Address);
        RaiseValueChanged(Address, raw, raw.ToString());
        // 更新 UI...
    }

    // 2. 主題變更（可選）
    protected override void OnThemeChanged(ResourceDictionary theme)
    {
        // 更新語意 Token
    }
}
```

不需要手動訂閱 `PlcContext.GlobalStatus`、`ScanUpdated`、`ConnectionEstablished`，基類已統一處理。

---

## BehaviorEventBus（SecuredButton 點擊橋接）

`SecuredButton` 不繼承 `PlcControlBase`（它是安全控件，不是 PLC 資料控件），但也需要觸發 Behavior。
透過靜態 `BehaviorEventBus.RaiseControlEventFired(id, "clicked", 0)` 橋接，讓 BehaviorEngine 收到點擊事件。

**位置：** `Stackdose.UI.Core/Helpers/BehaviorEventBus.cs`

---

## 相關文件

- `docs/refactor/B0-control-inventory.md` — 26 個控件完整 DP 清單
- `docs/kb/behavior-system.md` — BehaviorEngine 與 events[] JSON
