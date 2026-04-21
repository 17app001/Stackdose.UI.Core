# 專案依賴地圖

> 本文件記錄 Stackdose.UI.Core.sln 所有專案的依賴關係，包含跨 Repo 的外部引用。

---

## 完整依賴圖

```
Stackdose.UI.Core.sln
│
├── [外部 Repo] ../Stackdose.Platform/
│   ├── Stackdose.Abstractions      ← 所有介面定義（IPlcManager, IPrintHead, ILogService...）
│   ├── Stackdose.Core              ← enum、MachineState、工具類
│   ├── Stackdose.Hardware          ← PLC 硬體驅動實作（FX3U）
│   ├── Stackdose.Plc               ← PLC 輪詢實作
│   ├── Stackdose.PrintHead         ← Feiyang PrintHead 控制實作
│   ├── Stackdose.Logging           ← 日誌實作
│   └── Stackdose.Machine           ← 機台控制器實作
│
├── [外部 C++ SDK] ../../Sdk/FeiyangWrapper/
│   └── FeiyangWrapper.vcxproj      ← C++/CLI 介接層（條件引用 x64 Debug/Release DLL）
│
├── Stackdose.UI.Core               ← 核心WPF庫
│   ├── → Stackdose.Abstractions
│   ├── → Stackdose.Core
│   ├── → Stackdose.Hardware
│   ├── → Stackdose.PrintHead
│   ├── → FeiyangWrapper.dll（條件）
│   ├── NuGet: Dapper 2.1.66
│   ├── NuGet: Microsoft.Data.Sqlite 10.0.1
│   └── NuGet: System.DirectoryServices.AccountManagement 8.0.0
│
├── Stackdose.UI.Templates          ← Shell 布局元件
│   └── → Stackdose.UI.Core
│
├── Stackdose.App.ShellShared       ← 多App共用Shell
│   ├── → Stackdose.UI.Core
│   └── → Stackdose.UI.Templates
│
├── Stackdose.App.DeviceFramework   ← 設備App組裝框架
│   ├── → Stackdose.UI.Core
│   ├── → Stackdose.UI.Templates
│   └── → Stackdose.App.ShellShared
│
├── Stackdose.App.UbiDemo           ← UBI工業烤箱Demo
│   ├── → Stackdose.UI.Core
│   ├── → Stackdose.UI.Templates
│   ├── → Stackdose.App.ShellShared
│   └── → Stackdose.App.DeviceFramework
│
├── Stackdose.App.DesignRuntime     ← PLC連線真實執行環境 [開發驗證]
│   ├── → Stackdose.UI.Core
│   ├── → Stackdose.UI.Templates
│   ├── → Stackdose.App.DeviceFramework
│   └── → Stackdose.Tools.MachinePageDesigner
│
├── Stackdose.App.DesignPlayer      ← 量產交付：JSON + PLC + 登入管控 [✅ 完整]
│   ├── → Stackdose.UI.Core
│   ├── → Stackdose.UI.Templates
│   ├── → Stackdose.App.ShellShared
│   └── → Stackdose.App.DeviceFramework
│
├── Stackdose.Tools.MachinePageDesigner  ← 自由畫布設計器 [主力開發]
│   ├── → Stackdose.UI.Core
│   └── → Stackdose.App.DeviceFramework
│
├── Stackdose.Tools.DesignViewer    ← JSON即時預覽工具 [開發中]
│   ├── → Stackdose.UI.Core
│   └── → Stackdose.Tools.MachinePageDesigner
│
├── Stackdose.Tools.ProjectGenerator    ← CLI專案產生器（library）
│   └── → Stackdose.UI.Core
│
├── Stackdose.Tools.ProjectGeneratorUI  ← GUI專案產生器
│   ├── → Stackdose.UI.Core
│   ├── → Stackdose.UI.Templates
│   ├── → Stackdose.App.DeviceFramework
│   └── → Stackdose.Tools.ProjectGenerator
│
└── Stackdose.UI.Core.Tests         ← 單元測試
    └── → Stackdose.UI.Core
```

---

## 外部 Repo 詳細說明

### Stackdose.Platform

**位置：** `D:\工作區\Project\Stackdose.Platform\`
**Git Repo：** 獨立 Repo，有自己的 commit history

| 專案 | 最近異動 | 說明 |
|---|---|---|
| `Stackdose.Abstractions` | 2026-03-23 | 所有介面定義，任何變動都可能影響 UI.Core |
| `Stackdose.Core` | 2026-03-23 | enum、MachineState、工具類 |
| `Stackdose.Hardware` | 2026-03-23 | FX3U PLC 驅動實作 |
| `Stackdose.Plc` | 2026-03-20 | PLC 輪詢（BatchRead 最佳化） |
| `Stackdose.PrintHead` | 2026-03-23 | Feiyang SDK 包裝 |
| `Stackdose.Logging` | 2026-03-20 | 日誌實作 |
| `Stackdose.Machine` | 2026-03-20 | IMachineController 實作 |

### FeiyangWrapper SDK

**位置：** `D:\工作區\Sdk\FeiyangWrapper\`
**類型：** C++/CLI，需獨立 Build（vcxproj）
**版本：** SDK 2.3.1（`FeiyangSDK-2.3.1/`）
**注意：** 引用條件為 `Exists()`，沒有 DLL 不報編譯錯，但 PrintHead 功能失效

---

## 平台目標說明

所有專案統一：
- `TargetFramework: net8.0-windows`
- `PlatformTarget: x64`
- `UseWPF: true`

**切換 AnyCPU 會導致 FeiyangWrapper 無法載入。**

---

## NuGet 套件彙整

| 套件 | 版本 | 使用方 |
|---|---|---|
| Dapper | 2.1.66 | UI.Core（SQLite ORM） |
| Microsoft.Data.Sqlite | 10.0.1 | UI.Core（合規日誌DB） |
| System.DirectoryServices.AccountManagement | 8.0.0 | UI.Core（AD驗證） |
