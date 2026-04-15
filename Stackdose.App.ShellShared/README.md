# Stackdose.App.ShellShared

Shell App 的**共用底層服務**。負責從 `Config/` 目錄讀取設定、將機台資料綁定至 Shell 頁面，是所有 Shell 型 App 的共通基礎。

## 這是什麼

不是可執行的 App，而是各 Shell App 專案引用的**共用函式庫**。`DeviceFramework` 在底層依賴它；透過腳本產生的新 App 也自動依賴它。

## 依賴

- `Stackdose.UI.Core`
- `Stackdose.UI.Templates`

## 核心類別

| 類別 | 職責 |
|---|---|
| `ShellRuntimeHost` | App 啟動入口：載入 `app-meta.json` + `Machine*.config.json`，回傳 `ShellRuntimeContext` |
| `ShellRuntimeContext` | 執行期快取：持有 `MachineOverviewPage`、機台字典、Meta，並提供 `GetAlarmConfigFile` / `GetSensorConfigFile` 查詢 |
| `ShellAppMetaLoader` | 解析 `app-meta.json`（標題、選單設定） |
| `ShellConfigLoader` | 掃描 `Config/` 目錄，批次載入所有 `Machine*.config.json` |
| `ShellOverviewBinder` | 將 Meta 與機台設定套用至 `MachineOverviewPage` |
| `ShellMonitorAddressBuilder` | 從機台設定建立 PLC 監控地址清單 |

## 使用方式

```csharp
// MainWindow.xaml.cs（Shell App）
var ctx = ShellRuntimeHost.Start(shellContainer);
// ctx.Machines["MachineA"].xxx
```

`Config/` 目錄需放置：
- `app-meta.json`
- `Machine*.config.json`（一台機台一個檔案）
