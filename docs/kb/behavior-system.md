# Behavior Engine 系統

> B4/B5 產出。讓設計師在不寫 C# 的前提下，透過 JSON 設定控件的觸發反應邏輯。

---

## 1. 概念

```
PLC 值更新 / 按鈕點擊
  → PlcEventContext.ControlValueChanged
  → BehaviorEngine.Dispatch
    → 比對 events[].when 條件
    → 執行 do[] 動作清單
    → 每個 action 自動呼叫 ComplianceContext 寫稽核
```

---

## 2. JSON Schema（events[]）

`DesignerItemDefinition.events[]` 與 `PageDefinition.canvasItems[].events[]` 格式相同：

```json
"events": [
  {
    "on": "valueChanged",
    "when": { "op": ">", "value": 100 },
    "do": [
      { "action": "setProp",    "target": "Self", "prop": "Foreground", "value": "Status.Error" },
      { "action": "writePlc",   "address": "M0",  "value": 1 },
      { "action": "logAudit",   "message": "D100 超過 100" },
      { "action": "showDialog", "title": "警告",  "message": "溫度過高" },
      { "action": "navigate",   "page": "AlarmPage" },
      { "action": "setStatus",  "target": "SomeLabel", "value": "Error" }
    ]
  }
]
```

### 2.1 BehaviorEvent 欄位

| 欄位 | 型別 | 說明 |
|---|---|---|
| `on` | string | 觸發事件：`valueChanged`、`clicked` |
| `when` | BehaviorCondition? | 可省略（省略代表無條件觸發） |
| `do` | BehaviorAction[] | 依序執行的動作清單 |

### 2.2 BehaviorCondition

| 欄位 | 型別 | 說明 |
|---|---|---|
| `op` | string | `>`、`>=`、`<`、`<=`、`==`、`!=` |
| `value` | double | 比較值 |

### 2.3 BehaviorAction 欄位

| 欄位 | action 適用 | 說明 |
|---|---|---|
| `action` | 全部 | 動作識別名（大小寫不敏感） |
| `target` | setProp、setStatus | 控件 id 或 `"Self"`（觸發來源本身） |
| `prop` | setProp | 要修改的屬性名（例：`Foreground`） |
| `value` | setProp、writePlc、setStatus | 目標值 |
| `address` | writePlc | PLC 地址（例：`M0`、`D100`） |
| `message` | logAudit、showDialog | 訊息文字 |
| `title` | showDialog | 對話框標題 |
| `page` | navigate | 目標頁面 id |

---

## 3. 內建 Handler（6 個）

| ActionType | 功能 | 備註 |
|---|---|---|
| `SetProp` | 修改目標控件的屬性（反射） | `target` + `prop` + `value` |
| `WritePlc` | 寫入 PLC 地址 | 需注入 `PlcManager` |
| `LogAudit` | 寫入 ComplianceContext 稽核日誌 | FDA 21 CFR Part 11 |
| `ShowDialog` | 顯示 CyberMessageBox | UI Thread 自動處理 |
| `Navigate` | 切換頁面（Standard 模式） | 需注入 `Navigator` |
| `SetStatus` | 設定語意狀態（改 theme token） | 目前以 SetProp 實作 |

---

## 4. BehaviorEngine 使用方式

```csharp
var engine = new BehaviorEngine
{
    PlcManager  = plcManager,    // IPlcManager（nullable，WritePlc 需要）
    AuditLogger = msg => ComplianceContext.LogSystem(msg, LogLevel.Info),
    Navigator   = pageId => SwitchPage(pageId),  // Standard 模式多頁切換
};

// 每次載入新文件時呼叫
engine.BindDocument(doc.CanvasItems, controlMap);  // controlMap: id → FrameworkElement

// 頁面卸載時
engine.Dispose();
```

---

## 5. Standard 多頁模式（B7）

`DesignDocument.pages[]` 非空且 `shellMode == "Standard"` 時，DesignRuntime 自動：
1. 為每個 `PageDefinition` 建立獨立 Canvas
2. 從 `pages[].title` 生成 LeftNavigation 項目
3. 點擊 LeftNav → 切換頁面
4. 注入 `BehaviorEngine.Navigator` → `Navigate` action 可程式碼切換頁面

```json
{
  "shellMode": "Standard",
  "pages": [
    { "id": "Overview", "title": "概覽",  "canvasItems": [...] },
    { "id": "Alarm",    "title": "警報",  "canvasItems": [...] }
  ]
}
```

---

## 6. 擴充自訂 Handler

```csharp
public sealed class CloseValveHandler : IBehaviorActionHandler
{
    public string ActionType => "CloseValve";

    public void Execute(BehaviorActionContext ctx)
    {
        // ctx.PlcManager, ctx.AuditLogger, ctx.ControlResolver, ctx.Navigator
        ctx.PlcManager?.WriteAsync("Y0", 0);
        ctx.AuditLogger?.Invoke("CloseValve triggered");
    }
}

engine.Register(new CloseValveHandler());  // 覆寫同名 handler
```

---

## 7. 相關檔案

| 檔案 | 位置 |
|---|---|
| `BehaviorEvent/Condition/Action` | `UI.Core/Models/` |
| `BehaviorEngine` | `ShellShared/Behaviors/BehaviorEngine.cs` |
| `IBehaviorActionHandler` | `ShellShared/Behaviors/IBehaviorActionHandler.cs` |
| 六個內建 Handler | `ShellShared/Behaviors/Handlers/` |
| `BehaviorEventBus` | `UI.Core/Helpers/BehaviorEventBus.cs` |
| `PageDefinition` | `MachinePageDesigner/Models/PageDefinition.cs` |
