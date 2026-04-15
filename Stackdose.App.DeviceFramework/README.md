# Stackdose.App.DeviceFramework

設備 App 的**組裝框架層**。用 JSON 設定驅動 App 啟動、Shell 導航、設備頁資料綁定與 PLC 命令流程，讓新設備專案低程式碼上線。

## 這是什麼

不是單一設備 App，而是**打造多設備 App 的框架**。上層 App 專案引用它，透過設定檔即可組出完整介面。

## 依賴
- `Stackdose.UI.Core`
- `Stackdose.UI.Templates`
- `Stackdose.App.ShellShared`

## 核心類別

| 類別 | 職責 |
|---|---|
| `AppController` | App 生命週期（Start / Stop），上層 App 的唯一接觸點 |
| `RuntimeHost` | 掃描 Config/ 目錄，載入 app-meta.json + Machine*.config.json |
| `DeviceContextMapper` | JSON → DeviceContext（Labels、Commands、Modules） |
| `NavigationOrchestrator` | 統一頁面切換（Overview / Detail / Log / User / Settings） |
| `ProcessCommandService` | 寫入 PLC 命令地址並回傳執行結果 |
| `DynamicDevicePage` | 根據 DeviceContext 動態呈現頁面，零 C# 也可運作 |

## 快速啟動

```csharp
// MainWindow.xaml.cs
var controller = new AppController(this);
controller.Start();
```

Config/ 目錄放 `app-meta.json` 與 `Machine*.config.json` 即可自動載入。

## 擴充順序

1. 純 JSON + `DynamicDevicePage`（最快）
2. 覆寫 `IRuntimeMappingAdapter`
3. 覆寫 `RuntimeMapper.CreateDeviceContext`
4. `AppController.ConfigurePageFactory(...)` 換自訂頁面
5. `AppController.SettingsPage` 客製設定頁
