# Stackdose.App.UbiDemo

**Ubi 機台 App 參考實作**。示範如何在 DeviceFramework 基礎上建立客製化設備頁，包含自訂 ViewModel、Mapper、PrintHead 整合與自訂 PLC 位址映射。

## 這是什麼

一個完整可執行的 WPF App，同時也是**新 App 開發的範本**。當新設備需要超出 JSON-only 流程的能力時，參考此專案的擴充模式。

## 依賴

- `Stackdose.App.DeviceFramework`
- `Stackdose.App.ShellShared`
- `Stackdose.UI.Core`
- `Stackdose.UI.Templates`

## 結構

```
UbiDemo/
├── Models/          UbiMachineConfig, UbiDeviceContext（Ubi 專屬欄位）
├── Services/        UbiDeviceContextMapper, UbiFrameworkMappingAdapter
├── Pages/           UbiDevicePage.xaml（客製詳情頁）, SettingsPage.xaml
├── ViewModels/      UbiDevicePageViewModel, SettingsPageViewModel
├── Controls/        PlcBindableField.xaml（自訂控制項範例）
└── Config/          app-meta.json, MachineA.config.json
```

## 擴充模式示範

| 擴充點 | Ubi 的做法 |
|---|---|
| 自訂 DeviceContext | `UbiDeviceContext`（繼承基礎，加入 PrintHead、層架等欄位） |
| 自訂映射 | `UbiDeviceContextMapper.FromFrameworkContext()` |
| 自訂 Adapter | `UbiFrameworkMappingAdapter`（實作 `IRuntimeMappingAdapter`） |
| 自訂頁面 | `UbiDevicePage` + `UbiDevicePageViewModel` |

## 啟動

```csharp
// MainWindow.xaml.cs
var controller = new AppController(this);
controller.ConfigurePageFactory((ctx) => new UbiDevicePage(ctx));
controller.Start();
```
