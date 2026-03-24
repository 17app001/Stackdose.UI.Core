# Stackdose.App.DeviceFramework 使用指南

> 從零開始建立一個新裝置應用程式的完整說明。

---

## 目錄

1. [框架概覽](#1-框架概覽)
2. [架構圖](#2-架構圖)
3. [最小啟動：零程式碼方式（推薦）](#3-最小啟動零程式碼方式推薦)
4. [自訂頁面方式](#4-自訂頁面方式)
5. [Config JSON 格式說明](#5-config-json-格式說明)
6. [自訂擴充點](#6-自訂擴充點)
7. [框架提供的類別一覽](#7-框架提供的類別一覽)
8. [進階用法](#8-進階用法)
9. [實際範例：UbiDemo 的使用方式](#9-實際範例ubidemo-的使用方式)
10. [實際範例：Launcher 的使用方式](#10-實際範例launcher-的使用方式)
11. [常見問題](#11-常見問題)

---

## 1. 框架概覽

`Stackdose.App.DeviceFramework` 是一個 **WPF 裝置應用框架**，提供：

- ? Config 自動載入（`app-meta.json` + `Machine*.config.json`）
- ? Shell 導航（Overview / Detail / LogViewer / UserManagement / Settings）
- ? PLC 監控地址自動計算與註冊
- ? Overview 卡片即時更新（Running/Alarm/Batch/Recipe）
- ? Meta Hot Reload（開發時修改 `app-meta.json` 即時生效）
- ? 裝置頁面工廠（App 端注入自訂頁面）
- ? **DynamicDevicePage**（內建通用頁面，零程式碼即可使用）
- ? 動態 Labels / Commands（從 JSON Config 驅動 UI）
- ? Tags 自動匯入 Labels（`tags.status` + `tags.process` → `DeviceContext.Labels`）
- ? 通用 ViewModel / RelayCommand / ProcessState

**App 端有兩種模式：**

| 模式 | 需要寫的程式碼 | 適用情境 |
|------|--------------|---------|
| **最小啟動（推薦）** | 4 個檔案 + JSON Config | 新專案、快速驗證、通用設備 |
| **自訂頁面** | 6+ 個檔案 + JSON Config | 需要特殊 UI 佈局的設備 |

---

## 2. 架構圖

```
┌─────────────────────────────────────────────────────────┐
│  Your App (e.g. Stackdose.App.Launcher)                 │
│                                                         │
│  MainWindow.xaml.cs                                     │
│    └─ AppController                                     │
│         ├─ RuntimeHost          (載入 Config)            │
│         │    └─ RuntimeMapper   (地址映射)               │
│         │         └─ IRuntimeMappingAdapter (可自訂)     │
│         ├─ BootstrapService     (初始化 Shell)           │
│         ├─ MetaRuntimeService   (Meta 監控/Hot Reload)   │
│         ├─ DevicePageService    (裝置頁面快取)            │
│         │    └─ PageFactory     (你提供的頁面工廠)        │
│         ├─ NavigationOrchestrator (導航)                 │
│         └─ ShellCoordinator     (Shell UI 控制)          │
│                                                         │
│  Config/                                                │
│    ├─ app-meta.json            (App 設定)                │
│    ├─ MachineA.config.json     (機台 A 設定)             │
│    └─ MachineB.config.json     (機台 B 設定)             │
│                                                         │
│  （可選）                                                │
│  Pages/                                                 │
│    ├─ MyDevicePage.xaml         (自訂裝置頁面)            │
│    └─ MySettingsPage.xaml       (自訂設定頁面)            │
│  Services/                                              │
│    └─ MyMappingAdapter.cs       (自訂地址映射)            │
└─────────────────────────────────────────────────────────┘
         │ 參考
         ▼
┌─────────────────────────────────────────────────────────┐
│  Stackdose.App.DeviceFramework                          │
│    ├─ DynamicDevicePage (內建通用頁面)                    │
│    ├─ DevicePageViewModel (動態 Labels/Commands)         │
│    └─ ProcessCommandService (PLC 命令)                   │
│  Stackdose.UI.Core                                      │
│  Stackdose.UI.Templates                                 │
└─────────────────────────────────────────────────────────┘
```

---

## 3. 最小啟動：零程式碼方式（推薦）

使用框架內建的 `DynamicDevicePage`，**不需要寫任何裝置頁面**。
只需 **4 個程式碼檔案 + 2 個 JSON Config**。

### Step 1：建立 WPF 專案

```xml
<!-- Stackdose.App.MyDevice.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Stackdose.App.DeviceFramework\Stackdose.App.DeviceFramework.csproj" />
    <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
    <ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Config\**\*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### Step 2：App.xaml + App.xaml.cs

```xml
<!-- App.xaml -->
<Application x:Class="Stackdose.App.MyDevice.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/Stackdose.UI.Core;component/Themes/Theme.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

```csharp
// App.xaml.cs
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Templates.Helpers;
using System.Windows;

namespace Stackdose.App.MyDevice;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        bool loginSuccess = LoginDialog.ShowLoginDialog();
        if (!loginSuccess) { Shutdown(); return; }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
```

### Step 3：MainWindow.xaml + MainWindow.xaml.cs

```xml
<!-- MainWindow.xaml -->
<Window x:Class="Stackdose.App.MyDevice.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Custom="http://schemas.stackdose.com/templates"
        xmlns:TemplatePages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
        Title="My Device App"
        Height="900" Width="1600"
        WindowState="Maximized" WindowStyle="None">
    <Custom:MainContainer x:Name="MainShell"
                          IsShellMode="True"
                          HeaderDeviceName="MY DEVICE"
                          PageTitle="Machine Overview">
        <Custom:MainContainer.ShellContent>
            <TemplatePages:MachineOverviewPage />
        </Custom:MainContainer.ShellContent>
    </Custom:MainContainer>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using System.Windows;

namespace Stackdose.App.MyDevice;

public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.MyDevice");
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

        _controller.ConfigurePageFactory(
            ctx => { var p = new DynamicDevicePage(); p.SetContext(ctx); return p; },
            (page, ctx) => { if (page is DynamicDevicePage dp) dp.SetContext(ctx); });

        Loaded += (_, _) => _controller.Start();
        Unloaded += (_, _) => _controller.Dispose();
    }
}
```

### Step 4：建立 Config

```
Config/
├── app-meta.json
└── Machine1.config.json
```

**就這樣！不需要寫任何裝置頁面、ViewModel 或 Mapper。**

`DynamicDevicePage` 會自動：
- 顯示機台名稱和 Process 狀態
- 用 `ItemsControl` 動態渲染所有 `detailLabels` + `tags` 為即時 PlcLabel
- 用 `ItemsControl` 動態渲染所有 `commands` 為可點擊按鈕
- 處理 PLC 事件（Running / Completed / Alarm）
- 使用 `ProcessCommandService` 執行命令

### 最小啟動清單

| 檔案 | 行數 | 說明 |
|------|------|------|
| `App.xaml` + `App.xaml.cs` | 10 | 主題引用 |
| `MainWindow.xaml` + `MainWindow.xaml.cs` | 14 | Shell 容器 |
| `Config/app-meta.json` | ~30 | App 設定 |
| `Config/Machine1.config.json` | ~50 | 設備定義 |
| **合計** | **~144** | **完整可運行的設備 App** |

---

## 4. 自訂頁面方式

如果 `DynamicDevicePage` 的佈局不符需求，可以建立自訂頁面。

### MainWindow.xaml.cs 改為使用自訂頁面

```csharp
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.MyDevice.Pages;
using System.Windows;

namespace Stackdose.App.MyDevice;

public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.MyDevice");
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

        _controller.ConfigurePageFactory(
            ctx => { var p = new MyDevicePage(); p.SetContext(ctx); return p; },
            (page, ctx) => { if (page is MyDevicePage p) p.SetContext(ctx); });

        // （可選）設定 Settings 頁面
        // _controller.SettingsPage = new MySettingsPage();

        Loaded += (_, _) => _controller.Start();
        Unloaded += (_, _) => _controller.Dispose();
    }
}
```

### 自訂裝置頁面

**方式 A：使用框架的 DevicePageViewModel（推薦，動態 Labels/Commands）**

```csharp
// Pages/MyDevicePage.xaml.cs
using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;
using Stackdose.App.DeviceFramework.ViewModels;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.MyDevice.Pages;

public partial class MyDevicePage : UserControl
{
    private readonly DevicePageViewModel _viewModel = new();

    public MyDevicePage()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void SetContext(DeviceContext context)
    {
        _viewModel.ApplyDeviceContext(context);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered += OnPlcEvent;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PlcEventContext.EventTriggered -= OnPlcEvent;
    }

    private void OnPlcEvent(object? sender, PlcEventTriggeredEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnPlcEvent(sender, e));
            return;
        }

        switch (e.EventName)
        {
            case ProcessEventNames.Running:   _viewModel.MarkProcessRunning();   break;
            case ProcessEventNames.Completed: _viewModel.MarkProcessCompleted(); break;
            case ProcessEventNames.Alarm:     _viewModel.MarkProcessFaulted();   break;
        }
    }
}
```

**方式 B：自訂 ViewModel（需要硬編碼屬性時）**

繼承 `ViewModelBase` 並寫自己的屬性，或繼承 `DevicePageViewModel` 擴充。

---

## 5. Config JSON 格式說明

### app-meta.json

```jsonc
{
  // Shell Header 顯示的裝置名稱
  "headerDeviceName": "MY DEVICE",

  // 預設頁面標題
  "defaultPageTitle": "Machine Overview",

  // 是否使用框架 Shell 服務
  "useFrameworkShellServices": false,

  // 開發時啟用 Meta Hot Reload（修改此檔即時生效）
  "enableMetaHotReload": false,

  // Overview 卡片是否顯示 Alarm 計數
  "enableOverviewAlarmCount": true,

  // Overview 面板開關
  "showMachineCards": true,
  "showSoftwareInfo": true,
  "showLiveLog": true,
  "bottomPanelHeight": 440,
  "bottomLeftTitle": "Software Information",
  "bottomRightTitle": "Live Log",

  // 左側導航列
  "navigationItems": [
    { "title": "Machine Overview",  "navigationTarget": "MachineOverviewPage",  "requiredLevel": "Operator" },
    { "title": "Machine Detail",    "navigationTarget": "MachineDetailPage",    "requiredLevel": "Operator" },
    { "title": "Log Viewer",        "navigationTarget": "LogViewerPage",        "requiredLevel": "Instructor" },
    { "title": "User Management",   "navigationTarget": "UserManagementPage",   "requiredLevel": "Admin" },
    { "title": "Maintenance Mode",  "navigationTarget": "SettingsPage",         "requiredLevel": "SuperAdmin" }
  ],

  // Overview 下方軟體資訊
  "softwareInfoItems": [
    { "label": "Application", "value": "Stackdose.App.MyDevice" },
    { "label": "Version",     "value": "v1.0.0" }
  ]
}
```

**navigationTarget 可用值：**

| Target | 說明 |
|--------|------|
| `MachineOverviewPage` | Overview 首頁 |
| `MachineDetailPage` | 裝置詳情頁 |
| `LogViewerPage` | 日誌檢視器 |
| `UserManagementPage` | 使用者管理 |
| `SettingsPage` | 設定 / 維護模式 |

**requiredLevel 可用值：** `Operator` / `Instructor` / `Admin` / `SuperAdmin`

---

### Machine*.config.json

檔名必須符合 `Machine*.config.json` 模式（如 `Machine1.config.json`、`MachineA.config.json`、`Oven1.config.json`）。

```jsonc
{
  // ── 機台基本資訊 ──
  "machine": {
    "id": "M1",                    // 唯一識別 ID
    "name": "My Device A",         // 顯示名稱
    "enable": true                 // 是否啟用
  },

  // ── PLC 連線設定 ──
  "plc": {
    "ip": "192.168.1.100",
    "port": 3000,
    "pollIntervalMs": 200,         // 掃描間隔 (ms)
    "autoConnect": true,
    "monitorAddresses": [          // 額外需要監控的地址
      "D500,4",                    // D500~D503（連續 4 個）
      "M250"                       // 單一地址
    ]
  },

  // ── 命令地址（動態，key 自訂）──
  "commands": {
    "Start": "M300",
    "Pause": "M301",
    "Stop":  "M302",
    "EmergencyStop": "M303"        // 可任意新增
  },

  // ── 製程監控 ──
  "processMonitor": {
    "isRunning":   "M201",
    "isCompleted": "M203",
    "isAlarm":     "M202"
  },

  // ── 附屬 Config 檔案路徑 ──
  "alarmConfigFile":  "Config/Machine1/alarms.json",
  "sensorConfigFile": "Config/Machine1/sensors.json",
  "printHeadConfigs": [
    "Config/Machine1/head1.json",
    "Config/Machine1/head2.json"
  ],

  // ── 動態標籤（key = 顯示名稱, value = PLC 地址）──
  // 不同裝置可以有完全不同的 label
  "detailLabels": {
    "Oven Temp":      "D100",
    "Cooling Temp":   "D101",
    "Conveyor Speed": "D200",
    "Battery":        "D120",
    "Elapsed Time":   "D86"
  },

  // ── 功能模組宣告 ──
  "modules": [
    "processControl",
    "printHead",
    "alarm",
    "sensor"
  ],

  // ── Tag 區段（Overview 卡片 + 自動匯入 Labels）──
  // tags.status 和 tags.process 中 access="read" 的 tag
  // 會自動匯入 DeviceContext.Labels，不需要重複寫在 detailLabels 中。
  "tags": {
    "status": {
      "isRunning":  { "address": "M201", "type": "bool",  "access": "read" },
      "isAlarm":    { "address": "M202", "type": "bool",  "access": "read" },
      "heartbeat":  { "address": "D300", "type": "int16", "access": "read" }
    },
    "process": {
      "batchNo":    { "address": "D400", "type": "string", "access": "read", "length": 8 },
      "recipeNo":   { "address": "D410", "type": "string", "access": "read", "length": 8 },
      "nozzleTemp": { "address": "D420", "type": "int16",  "access": "read" }
    }
  }
}
```

**重點：**
- `commands`：字典型，key 名稱自訂，框架會自動產生按鈕
- `detailLabels`：字典型，key 名稱自訂，框架會自動產生 PlcLabel
- `tags`：`status` + `process` 中 `access: "read"` 的 tag 會**自動匯入** `DeviceContext.Labels`（不需要重複寫在 `detailLabels` 中）
- `modules`：宣告此設備啟用的功能模組，App 端可據此決定顯示哪些 UI 區塊

---

## 6. 自訂擴充點

### 6.1 IRuntimeMappingAdapter（地址映射）

當預設的 `DefaultRuntimeMappingAdapter` 不夠用時（例如需要 fallback 規則），建立自訂 adapter：

```csharp
using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;

internal sealed class MyMappingAdapter : DefaultRuntimeMappingAdapter
{
    public override string GetAlarmConfigFile(MachineConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.AlarmConfigFile))
            return base.GetAlarmConfigFile(config);

        // 自訂 fallback 邏輯
        return config.Machine.Id switch
        {
            "M1" => Path.Combine(AppContext.BaseDirectory, "Config/Device1/alarms.json"),
            _    => string.Empty
        };
    }
}
```

使用方式：

```csharp
var adapter = new MyMappingAdapter();
var runtimeMapper = new RuntimeMapper(adapter);
var runtimeHost = new RuntimeHost(runtimeMapper, "Stackdose.App.MyDevice");
var controller = new AppController(MainShell, Dispatcher, runtimeHost);
```

**可覆寫的方法：**

| 方法 | 用途 |
|------|------|
| `GetTagAddress` | Tag 地址解析 |
| `GetDetailLabelAddress` | DetailLabel 地址解析（帶 fallback） |
| `GetAlarmConfigFile` | Alarm 設定檔路徑 |
| `GetSensorConfigFile` | Sensor 設定檔路徑 |
| `GetPrintHeadConfigFiles` | PrintHead 設定檔路徑列表 |
| `GetDetailLabelAddresses` | 所有 DetailLabel 的 PLC 地址集合 |
| `GetManualPlcMonitorAddresses` | 手動監控地址展開 |
| `GetMachineAlertAddresses` | 從 Alarm/Sensor JSON 收集地址 |
| `LoadAlarmBitPoints` | 載入 Alarm bit 點位 |

### 6.2 RuntimeMapper（DeviceContext 建立）

如果需要額外自訂 `DeviceContext`，可以覆寫 `CreateDeviceContext`：

```csharp
internal sealed class MyRuntimeMapper : RuntimeMapper
{
    public MyRuntimeMapper(IRuntimeMappingAdapter adapter) : base(adapter) { }

    public override DeviceContext CreateDeviceContext(MachineConfig config)
    {
        var context = base.CreateDeviceContext(config);
        context.Labels["customField"] = new DeviceLabelInfo("D999");
        return context;
    }
}
```

> **注意：** 大部分情況下不需要覆寫 RuntimeMapper。框架現在會自動將 `tags.status` 和 `tags.process` 中 `access: "read"` 的 tag 匯入 Labels。只有需要加入 JSON Config 中不存在的額外欄位時才需要覆寫。

### 6.3 Settings 頁面

```csharp
_controller.SettingsPage = new MySettingsPage();

_controller.OnSettingsNavigating = (page, runtime, machineId) =>
{
    if (page is MySettingsPage sp)
    {
        sp.SetMonitorAddresses(runtime.OverviewPage.PlcMonitorAddresses);
        sp.SetMachines(runtime.Machines, runtime.ConfigDirectory, machineId);
    }
};
```

### 6.4 ProcessCommandService（PLC 命令）

框架提供 `ProcessCommandService` 可直接使用：

```csharp
var commandService = new ProcessCommandService();
var result = await commandService.ExecuteCommandAsync(
    machineId: "M1",
    machineName: "My Device A",
    commandName: "Start",
    commandAddress: "M300");

// result.Success / result.State / result.Message
```

---

## 7. 框架提供的類別一覽

### Models

| 類別 | 說明 |
|------|------|
| `MachineConfig` | 機台 JSON 反序列化模型 |
| `AppMeta` | app-meta.json 反序列化模型 |
| `DeviceContext` | 傳給裝置頁面的上下文（含 Labels / Commands / Modules） |
| `DeviceLabelInfo` | 單一動態標籤描述（Address / DataType / Divisor 等） |
| `ProcessState` | 製程狀態列舉（Idle / Starting / Running / Completed / Stopped / Faulted） |
| `ProcessExecutionResult` | 命令執行結果 |

### Pages

| 類別 | 說明 |
|------|------|
| `DynamicDevicePage` | **內建通用裝置頁面** — 動態渲染 Labels / Commands，零程式碼即可使用 |

### Services

| 類別 | 說明 |
|------|------|
| `AppController` | **主控制器**，App 端唯一需要直接使用的類別 |
| `RuntimeHost` | Config 載入 + Overview 初始化 |
| `RuntimeMapper` | 地址映射 + DeviceContext 建立（可繼承） |
| `DefaultRuntimeMappingAdapter` | 預設地址映射（可繼承覆寫） |
| `IRuntimeMappingAdapter` | 地址映射介面 |
| `ProcessCommandService` | PLC 命令執行（可繼承覆寫） |
| `ProcessEventNames` | 製程事件名稱常數（`Running` / `Completed` / `Alarm`） |
| `ConfigLoader` | Config JSON 載入 |
| `DeviceContextMapper` | MachineConfig → DeviceContext 轉換（含 Tags 自動匯入） |
| `MonitorAddressBuilder` | 自動收集所有 PLC 監控地址 |

### ViewModels

| 類別 | 說明 |
|------|------|
| `ViewModelBase` | INPC 基底類別（提供 `SetProperty`） |
| `RelayCommand` | ICommand 實作 |
| `DevicePageViewModel` | 通用裝置頁面 ViewModel（含動態 Labels / Commands） |
| `DeviceLabelViewModel` | 單一動態標籤 ViewModel |
| `DeviceCommandViewModel` | 單一動態命令 ViewModel |

---

## 8. 進階用法

### 8.1 AppController 生命週期

```
建構式 → ConfigurePageFactory → Start() → ... 使用中 ... → Dispose()
         SettingsPage = ...
         OnSettingsNavigating = ...
```

- `Start()` 和 `Stop()` 是 `virtual`，可在子類別覆寫
- `Dispose()` 會自動呼叫 `Stop()`

### 8.2 多機台

JSON 中每個 `Machine*.config.json` 就是一台機器，框架自動：
- 在 Overview 建立對應的 `MachineOverviewCard`
- Header 下拉選單顯示所有機台
- 點擊卡片或選擇機台時，透過 `PageFactory` 建立/切換裝置頁面

### 8.3 DeviceContext 的 Labels 和 Commands

`DeviceContext.Labels` 和 `DeviceContext.Commands` 是 **動態字典**，來源有兩個：

| 來源 | 對應 JSON | 說明 |
|------|----------|------|
| `detailLabels` | `"detailLabels": { "key": "address" }` | 手動定義的標籤 |
| `tags` (自動匯入) | `"tags": { "status": {...}, "process": {...} }` | `access: "read"` 的 tag 自動加入 |

**優先順序：** `detailLabels` 中同名的 key 會優先於 `tags` 中的 key。

使用 `DevicePageViewModel` 時，框架會自動將它們轉為 `ObservableCollection<DeviceLabelViewModel>` 和 `ObservableCollection<DeviceCommandViewModel>`，可用 `ItemsControl` 在 XAML 中自動渲染。

### 8.4 ProcessEventNames

框架定義了三個標準事件名稱，用於 `PlcEventContext.EventTriggered`：

```csharp
ProcessEventNames.Running   // "ProcessRunning"
ProcessEventNames.Completed // "ProcessCompleted"
ProcessEventNames.Alarm     // "ProcessAlarm"
```

### 8.5 專案引用

**最小引用（推薦）：**

```xml
<ProjectReference Include="..\Stackdose.App.DeviceFramework\Stackdose.App.DeviceFramework.csproj" />
<ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
<ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
```

**如果用到 PrintHead 硬體（例如飛揚噴頭），額外加：**

```xml
<ProjectReference Include="..\..\Sdk\FeiyangWrapper\FeiyangWrapper\FeiyangWrapper.vcxproj" />
```

> **注意：** 不再需要引用 `Stackdose.App.ShellShared`。

---

## 9. 實際範例：UbiDemo 的使用方式

UbiDemo 是使用 DeviceFramework 的 **自訂頁面模式** 專案（因為 XAML 佈局是遷移前的遺產）。

```
Stackdose.App.UbiDemo/
├── App.xaml / App.xaml.cs                  # 啟動 + 主題 + 登入
├── MainWindow.xaml / MainWindow.xaml.cs     # AppController 初始化
├── Config/
│   ├── app-meta.json
│   ├── MachineA.config.json
│   └── MachineB.config.json
├── Pages/
│   ├── UbiDevicePage.xaml / .cs            # 自訂裝置頁面
│   └── SettingsPage.xaml / .cs             # 自訂設定頁面
├── ViewModels/
│   ├── UbiDevicePageViewModel.cs           # 自訂 ViewModel（硬編碼屬性）
│   └── SettingsPageViewModel.cs
├── Models/
│   └── UbiMachineConfig.cs                 # 只含 UbiDeviceContext
├── Controls/
│   └── PlcBindableField.xaml / .cs         # 自訂控制項
└── Services/
    ├── UbiFrameworkMappingAdapter.cs        # IRuntimeMappingAdapter 實作（fallback 規則）
    └── UbiDeviceContextMapper.cs            # 框架 DeviceContext → Ubi 本地型別
```

**UbiDemo 的 MainWindow.xaml.cs：**

```csharp
var adapter = new UbiFrameworkMappingAdapter();
var runtimeMapper = new RuntimeMapper(adapter);
var runtimeHost = new RuntimeHost(runtimeMapper, "Stackdose.App.UbiDemo");

_controller = new AppController(MainShell, Dispatcher, runtimeHost);
_controller.SettingsPage = new SettingsPage();

_controller.ConfigurePageFactory(
    ctx => { var p = new UbiDevicePage(); p.SetDeviceContext(UbiDeviceContextMapper.FromFrameworkContext(ctx)); return p; },
    (page, ctx) => { if (page is UbiDevicePage p) p.SetDeviceContext(UbiDeviceContextMapper.FromFrameworkContext(ctx)); });

_controller.OnSettingsNavigating = (page, runtime, machineId) => { ... };
```

---

## 10. 實際範例：Launcher 的使用方式

Launcher 是使用 DeviceFramework 的 **最小啟動模式** 專案 — 零設備程式碼。

```
Stackdose.App.Launcher/
├── App.xaml / App.xaml.cs                  # 啟動 + 主題 + 登入（模板程式碼）
├── MainWindow.xaml / MainWindow.xaml.cs     # AppController + DynamicDevicePage
└── Config/
    ├── app-meta.json                       # 改這裡 = 改 App 外觀
    └── Oven1.config.json                   # 改這裡 = 改設備行為
```

**Launcher 的 MainWindow.xaml.cs（完整，22 行）：**

```csharp
using Stackdose.App.DeviceFramework.Pages;
using Stackdose.App.DeviceFramework.Services;
using System.Windows;

namespace Stackdose.App.Launcher;

public partial class MainWindow : Window
{
    private readonly AppController _controller;

    public MainWindow()
    {
        InitializeComponent();

        var runtimeHost = new RuntimeHost(projectFolderName: "Stackdose.App.Launcher");
        _controller = new AppController(MainShell, Dispatcher, runtimeHost);

        _controller.ConfigurePageFactory(
            ctx => { var p = new DynamicDevicePage(); p.SetContext(ctx); return p; },
            (page, ctx) => { if (page is DynamicDevicePage dp) dp.SetContext(ctx); });

        Loaded += (_, _) => _controller.Start();
        Unloaded += (_, _) => _controller.Dispose();
    }
}
```

**要新增一台機器？** 只需在 `Config/` 新增一個 `*.config.json`，重啟即可。

---

## 11. 常見問題

### Q: 最小的新專案需要幾個檔案？

**使用 DynamicDevicePage（推薦）：4 個程式碼檔 + 2 個 JSON = 6 個檔案**

| 檔案 | 用途 |
|------|------|
| `App.xaml` + `App.xaml.cs` | 主題 + 登入 |
| `MainWindow.xaml` + `MainWindow.xaml.cs` | Shell + AppController |
| `Config/app-meta.json` | App 設定 |
| `Config/Machine1.config.json` | 設備定義 |

**使用自訂頁面：再加 2 個檔案（Page XAML + code-behind）**

### Q: 不需要自訂 MappingAdapter 嗎？

如果你的 JSON Config 已經填寫完整（alarmConfigFile、sensorConfigFile、printHeadConfigs 都有值），`DefaultRuntimeMappingAdapter` 就夠用了，不需要覆寫。

### Q: Tags 和 DetailLabels 有什麼差別？

| 欄位 | 功能 | 自動匯入 Labels？ |
|------|------|-----------------|
| `detailLabels` | 定義 UI 顯示的標籤（key = 顯示名稱） | ? 是 |
| `tags` | 定義 PLC Tag（用於 Overview 卡片更新） | ? 是（`access: "read"` 的 tag 自動匯入） |

兩者都會出現在 `DeviceContext.Labels` 中。`detailLabels` 中同名 key 優先。

### Q: 可以不用 Settings 頁面嗎？

可以，不設定 `_controller.SettingsPage` 即可。框架會用空白 `UserControl` 作為預設。也可以在 `app-meta.json` 的 `navigationItems` 中移除 `SettingsPage` 項目。

### Q: Labels / Commands 的 key 命名有限制嗎？

沒有限制，完全自訂。框架會自動：
- 將 camelCase/PascalCase 轉為 "Title Case" 顯示名稱
- 根據命令名稱推斷按鈕主題色（含 "start" → 綠色, 含 "stop" → 紅色 等）

### Q: 如何新增一台機器？

在 `Config/` 資料夾新增一個 `Machine*.config.json`（或任何 `*.config.json`）檔案即可，框架會自動偵測並載入。

### Q: DynamicDevicePage 和自訂頁面可以混用嗎？

可以。在 `ConfigurePageFactory` 中根據 `DeviceContext` 的屬性決定回傳哪種頁面：

```csharp
_controller.ConfigurePageFactory(
    ctx => ctx.EnabledModules.Contains("printHead")
        ? new MyPrintHeadDevicePage(ctx)   // 有噴頭的用自訂頁面
        : new DynamicDevicePage().Also(p => p.SetContext(ctx)),  // 其他用通用頁面
    ...);
```

### Q: 需要引用 Stackdose.App.ShellShared 嗎？

**不需要。** `ShellShared` 是舊框架，`DeviceFramework` 已完全取代它。
