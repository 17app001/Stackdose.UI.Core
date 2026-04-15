# DeviceFramework 新人指南

> 取代原 `DeviceFramework/docs/DeviceFramework-Guide.md`（已刪除，因亂碼不可用）。
> 本文是 DeviceFramework 的完整上手文件，從「什麼是 DeviceFramework」到「我需要寫程式嗎」都有答案。

---

## 什麼是 DeviceFramework？

`Stackdose.App.DeviceFramework` 是一個**組裝框架**，不是單一 App。
上層 App 引用它，透過 JSON 設定檔即可組出完整的設備監控介面，通常不需要寫程式碼。

```
你的新 App（Stackdose.App.YourMachine）
  ↓ 引用
DeviceFramework    ← 框架層（JSON 驅動）
  ↓ 引用
UI.Core / Templates / ShellShared
```

---

## 核心觀念

### DeviceContext（設備上下文）

JSON 設定被解析後，框架會建立一個 `DeviceContext` 物件，包含：

- `MachineId` / `MachineName` — 機台識別
- `Labels` — PLC 位址對應的顯示欄位（batchNo、recipeNo、nozzleTemp…）
- `Commands` — 控制命令（Start、Pause、Stop…）
- `AlarmConfigFile` / `SensorConfigFile` — 警報/感測器設定路徑
- `RunningAddress` / `AlarmAddress` — 狀態位元地址

### DynamicDevicePage（動態設備頁）

框架預設頁面。根據 `DeviceContext` 自動渲染 Labels 列表與命令按鈕，**零 C# 即可運作**。

---

## 設定檔格式

### `app-meta.json`（放在 `Config/` 目錄）

```json
{
  "headerDeviceName": "MY DEVICE",
  "defaultPageTitle": "機台監控",
  "version": "v1.0.0"
}
```

### `Machine*.config.json`（每台機台一個檔案）

```json
{
  "machine": {
    "id": "MachineA",
    "name": "A 號機",
    "enable": true
  },
  "plc": {
    "ip": "192.168.1.100",
    "port": 3000,
    "pollIntervalMs": 200
  },
  "alarmConfigFile": "Config/MachineA.alarm.json",
  "sensorConfigFile": "Config/MachineA.sensor.json",
  "tags": {
    "status": {
      "isRunning": "M0",
      "isAlarm": "M1"
    },
    "process": {
      "batchNo": "D100",
      "recipeNo": "D101",
      "nozzleTemp": "D200"
    }
  },
  "commands": {
    "Start": "M10",
    "Pause": "M11",
    "Stop": "M12"
  }
}
```

**最小必要欄位（JSON-only 流程）：**

| 欄位 | 說明 |
|---|---|
| `machine.id` / `machine.name` / `machine.enable` | 機台基本識別 |
| `plc.ip` / `plc.port` / `plc.pollIntervalMs` | PLC 連線參數 |
| `alarmConfigFile` / `sensorConfigFile` | 警報/感測器設定 |
| `tags.status.isRunning` / `tags.status.isAlarm` | 狀態位元 |
| `tags.process.batchNo` / `tags.process.recipeNo` / `tags.process.nozzleTemp` | 製程數據 |

---

## 啟動程式碼

```csharp
// MainWindow.xaml.cs — 最簡單的啟動方式（完全 JSON 驅動）
var controller = new AppController(this);
controller.Start();
```

這樣就夠了。`Config/` 目錄有設定檔就能跑。

---

## 我需要寫程式嗎？

| 情境 | 需要程式碼？ | 怎麼做 |
|---|---|---|
| 顯示 PLC 數值、警報、感測器 | 不需要 | 改 JSON 設定檔即可 |
| 自訂 detail page 綁定 | 不需要 | `app-meta.json` 設定 `detailPage` |
| 自訂 Labels 映射邏輯 | 需要（輕量） | 覆寫 `IRuntimeMappingAdapter` |
| 客製化 DeviceContext 欄位 | 需要 | 覆寫 `RuntimeMapper.CreateDeviceContext` |
| 完全客製詳情頁面 | 需要 | `AppController.ConfigurePageFactory(...)` |
| 客製設定頁面 | 需要 | `AppController.SettingsPage = new YourSettingsPage()` |

**建議順序：先改 JSON，能跑就不寫程式。** 需要擴充時再按上面順序往下走。

---

## 擴充範例（覆寫 Adapter）

```csharp
// 覆寫 Labels 映射，把自訂欄位鍵對應到非標準 PLC 位址
public class MyMappingAdapter : DefaultRuntimeMappingAdapter
{
    protected override void MapLabels(MachineConfig config, DeviceContext ctx)
    {
        base.MapLabels(config, ctx);
        // 加入自訂欄位
        ctx.Labels["motorSpeed"] = new DeviceLabelInfo { Address = "D300", DisplayName = "馬達轉速" };
    }
}

// MainWindow.xaml.cs
var controller = new AppController(this);
controller.MappingAdapter = new MyMappingAdapter();
controller.Start();
```

---

## 完整擴充案例：UbiDemo

`Stackdose.App.UbiDemo` 是完整的擴充範例，展示了：
- 自訂 `UbiDeviceContext`（加入 PrintHead、層架欄位）
- 自訂 `UbiDeviceContextMapper`
- 自訂 `UbiFrameworkMappingAdapter`
- 自訂 `UbiDevicePage` + `UbiDevicePageViewModel`

新成員閱讀 `UbiDemo/Services/UbiDeviceContextMapper.cs` 是理解映射機制最快的方式。

---

## 快速建立新 App

使用腳本，一行命令產生可執行的骨架：

```powershell
# 從方案根目錄執行
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourMachine" -DestinationRoot .
```

詳見 `docs/kb/quickstart.md`。

---

## 常見問題

**Q：Config/ 目錄放在哪裡？**
A：放在 App 的輸出目錄（即 `bin/Debug/net8.0-windows/Config/`）。Visual Studio 需在 csproj 設定 `CopyToOutputDirectory`。

**Q：機台名稱不顯示？**
A：確認 `machine.enable = true`，且 `app-meta.json` 路徑正確。

**Q：PLC 連不上？**
A：先用 `DesignRuntime` 勾選「模擬器模式」確認 App 架構正確，排除 PLC 網路問題。

**Q：新增一個自訂欄位顯示在 DynamicDevicePage？**
A：在 `tags.process` 下新增鍵值對即可（key 為任意字串，value 為 PLC 位址）。框架會自動顯示。
