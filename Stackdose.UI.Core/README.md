# Stackdose.UI.Core 使用說明文件

> **WPF 工業控制 UI 元件庫 - 符合 FDA 21 CFR Part 11 法規要求**  
> Version: 1.0.0  
> Framework: .NET 8.0 (WPF)

---

## ?? 目錄

- [快速開始](#快速開始)
- [核心元件](#核心元件)
  - [CyberFrame](#1-cyberframe---主視窗框架)
  - [PlcStatus](#2-plcstatus---plc-連線狀態)
  - [PlcLabel](#3-plclabel---plc-數值顯示)
  - [PlcDeviceEditor](#4-plcdeviceeditor---plc-讀寫編輯器)
  - [PlcEventTrigger](#5-plceventtrigger---plc-事件觸發器)
  - [SensorViewer](#6-sensorviewer---感測器監控面板)
  - [LiveLogViewer](#7-livelogviewer---即時日誌檢視器)
  - [SecuredButton](#8-securedbutton---權限控制按鈕)
  - [LoginDialog](#9-logindialog---登入對話框)
  - [CyberMessageBox](#10-cybermessagebox---訊息對話框)
- [核心系統](#核心系統)
  - [SecurityContext](#1-securitycontext---權限管理)
  - [ComplianceContext](#2-compliancecontext---法規合規引擎)
  - [PlcContext](#3-plccontext---plc-全域上下文)
  - [SensorContext](#4-sensorcontext---感測器管理)
- [主題系統](#主題系統)
- [資料庫結構](#資料庫結構)
- [最佳實踐](#最佳實踐)
- [常見問題](#常見問題)

---

## ?? 快速開始

### 1. 安裝套件

在您的 WPF 專案中引用 `Stackdose.UI.Core`：

```xml
<ItemGroup>
  <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
</ItemGroup>
```

### 2. 設定 App.xaml

在 `App.xaml` 中載入主題資源：

```xml
<Application x:Class="YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- 載入 Dark 主題 (預設) -->
                <ResourceDictionary Source="/Stackdose.UI.Core;component/Themes/Colors.xaml"/>
                
                <!-- 或載入 Light 主題 -->
                <!-- <ResourceDictionary Source="/Stackdose.UI.Core;component/Themes/LightColors.xaml"/> -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 3. 設定 MainWindow.xaml

加入 XML 命名空間並使用元件：

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Custom="http://schemas.stackdose.com/wpf"
        Title="工業控制系統" Width="1200" Height="800">
    <Grid>
        <!-- 主框架 -->
        <Custom:CyberFrame Title="MODEL-S">
            <!-- 您的內容 -->
        </Custom:CyberFrame>
        
        <!-- PLC 連線狀態 -->
        <Custom:PlcStatus x:Name="MainPlc"
                         IpAddress="192.168.1.1" 
                         Port="3000"/>
    </Grid>
</Window>
```

### 4. 初始化權限系統 (MainWindow.xaml.cs)

```csharp
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // 快速登入 (測試用)
        SecurityContext.QuickLogin(AccessLevel.Engineer);
        
        // 或顯示登入對話框 (正式環境)
        // bool loginSuccess = LoginDialog.ShowLoginDialog();
    }
}
```

---

## ?? 核心元件

### 1. CyberFrame - 主視窗框架

**用途：** 提供統一的賽博風格主視窗框架，包含標題列、時間顯示、使用者資訊、主題切換和登出按鈕。

#### 基本用法

```xml
<Custom:CyberFrame Title="生產監控系統">
    <Grid>
        <!-- 您的頁面內容 -->
    </Grid>
</Custom:CyberFrame>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `Title` | `string` | 標題文字 | `"SYSTEM"` |

#### 功能特色

- ? 即時時鐘顯示 (HH:mm:ss)
- ? 使用者名稱和權限等級顯示
- ? 主題切換按鈕 (Dark/Light)
- ? 登出按鈕（含發光動畫效果）
- ? 自動填滿父容器

---

### 2. PlcStatus - PLC 連線狀態

**用途：** 顯示 PLC 連線狀態，支援自動連線、手動重連、全域管理。

#### 基本用法

```xml
<!-- 單一 PLC (自動設為全域) -->
<Custom:PlcStatus x:Name="MainPlc"
                 IpAddress="192.168.1.1" 
                 Port="3000"
                 AutoConnect="True"
                 ScanInterval="150"/>
```

#### 多 PLC 配置

```xml
<!-- 主 PLC (全域) -->
<Custom:PlcStatus x:Name="MainPlc"
                 IpAddress="192.168.1.1" 
                 Port="3000"
                 IsGlobal="True"/>

<!-- 副 PLC (非全域) -->
<Custom:PlcStatus x:Name="SubPlc"
                 IpAddress="192.168.1.2" 
                 Port="3000"
                 IsGlobal="False"/>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `IpAddress` | `string` | PLC IP 位址 | `"127.0.0.1"` |
| `Port` | `int` | PLC 通訊埠 | `502` |
| `AutoConnect` | `bool` | 載入時自動連線 | `true` |
| `ScanInterval` | `int` | 掃描間隔 (毫秒) | `150` |
| `IsGlobal` | `bool` | 是否設為全域 PLC | `true` |
| `MonitorAddress` | `string` | 監控位址 (檢測連線) | `null` |
| `MonitorLength` | `int` | 監控長度 | `1` |
| `MaxRetryCount` | `int` | 最大重試次數 | `3` |

#### 事件

```csharp
// 連線成功事件
MainPlc.ConnectionEstablished += (manager) => 
{
    // PLC 連線成功後的處理
};

// 掃描更新事件
MainPlc.ScanUpdated += (manager) => 
{
    // 每次掃描後觸發
};
```

#### Code-Behind 使用

```csharp
// 手動連線
await MainPlc.ConnectAsync();

// 手動斷線
MainPlc.Disconnect();

// 取得 PLC Manager
var manager = MainPlc.CurrentManager;
if (manager != null && manager.IsConnected)
{
    // 讀取數據
    var value = await manager.ReadAsync("D100,10");
    
    // 寫入數據
    await manager.WriteAsync("M100,1");
}

// 透過全域存取
var globalManager = PlcContext.GlobalStatus?.CurrentManager;
```

---

### 3. PlcLabel - PLC 數值顯示

**用途：** 即時顯示 PLC 位址的數值，支援自動格式化、除法運算、數據記錄。

#### 基本用法

```xml
<!-- 顯示 Word (16-bit 整數) -->
<Custom:PlcLabel Label="溫度" 
                Address="D100" 
                DataType="Word"/>

<!-- 顯示浮點數 (自動除以 10) -->
<Custom:PlcLabel Label="壓力" 
                Address="D200" 
                DataType="Word"
                Divisor="10"
                StringFormat="F1"/>

<!-- 顯示 Bit (ON/OFF) -->
<Custom:PlcLabel Label="啟動狀態" 
                Address="M100" 
                DataType="Bit"/>

<!-- 顯示 Word 中的特定 Bit -->
<Custom:PlcLabel Label="安全門" 
                Address="D100" 
                DataType="Bit"
                BitIndex="5"/>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `Label` | `string` | 標籤文字 | `"Label"` |
| `Address` | `string` | PLC 位址 | `"D0"` |
| `DataType` | `PlcDataType` | 數據類型 | `Word` |
| `BitIndex` | `int` | Bit 索引 (0-15) | `-1` |
| `Divisor` | `double` | 除數 | `1.0` |
| `StringFormat` | `string` | 數值格式 | `"F1"` |
| `DefaultValue` | `string` | 預設顯示值 | `"00000"` |
| `TargetStatus` | `PlcStatus` | 指定 PLC | `null` (自動) |
| `EnableDataLog` | `bool` | 啟用數據記錄 | `false` |
| `EnableAuditTrail` | `bool` | 啟用審計追蹤 | `false` |
| `ShowLog` | `bool` | 顯示日誌到 UI | `true` |

#### 數據類型

```csharp
public enum PlcDataType
{
    Bit,    // 顯示 ON/OFF
    Word,   // 16-bit 整數
    DWord,  // 32-bit 整數
    Float   // 32-bit 浮點數
}
```

#### 多 PLC 配置

```xml
<!-- 綁定到特定 PLC -->
<Custom:PlcLabel Label="主機溫度" 
                Address="D100" 
                TargetStatus="{Binding ElementName=MainPlc}"/>

<Custom:PlcLabel Label="副機溫度" 
                Address="D200" 
                TargetStatus="{Binding ElementName=SubPlc}"/>
```

#### 事件處理

```csharp
// XAML
<Custom:PlcLabel x:Name="TempLabel" 
                Label="溫度" 
                Address="D100"/>

// Code-Behind
TempLabel.ValueChanged += (sender, e) =>
{
    var value = e.Value;      // object? (原始值)
    var display = e.DisplayText;  // string (格式化後)
    
    // 根據溫度執行動作
    if (double.TryParse(display, out double temp))
    {
        if (temp > 100)
        {
            CyberMessageBox.Show("溫度過高！", "警告");
        }
    }
};
```

#### 自動數據記錄

```xml
<!-- 啟用生產數據記錄 (寫入 SQLite DataLogs) -->
<Custom:PlcLabel Label="溫度" 
                Address="D100" 
                EnableDataLog="True"/>

<!-- 啟用審計追蹤 (寫入 SQLite AuditTrails) -->
<Custom:PlcLabel Label="關鍵參數" 
                Address="D200" 
                EnableAuditTrail="True"
                ShowLog="True"/>
```

---

### 4. PlcDeviceEditor - PLC 讀寫編輯器

**用途：** 手動讀取和寫入 PLC 位址，支援權限控制、Audit Trail 記錄。

#### 基本用法

```xml
<Custom:PlcDeviceEditor Label="手動操作區"/>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `Label` | `string` | 標題文字 | `"Device Editor"` |
| `Address` | `string` | 預設位址 | `""` |
| `Value` | `string` | 預設值 | `""` |

#### 權限控制

PlcDeviceEditor 需要 **Engineer** 權限才能使用：

```csharp
// 權限不足時會自動鎖定
// 顯示 "[LOCK]" 標記
// 按鈕無法點擊
```

#### 支援格式

| 格式 | 範例 | 說明 |
|------|------|------|
| **Bit** | `M100` | 讀寫單一 Bit (0/1) |
| **Word** | `D100` | 讀寫 16-bit 整數 |
| **Word Bit** | `D100.5` 或 `R2002,0` | 讀寫 Word 中的特定 Bit |
| **Multi-Word** | `D100,10` | 讀取 10 個連續 Word |

#### 功能特色

- ? 自動驗證位址格式
- ? 讀取/寫入自動記錄到 Audit Trail
- ? 權限不足時自動鎖定
- ? 操作成功/失敗提示

---

### 5. PlcEventTrigger - PLC 事件觸發器

**用途：** 監控 PLC 位址，當數值符合條件時自動觸發事件。

#### 基本用法

```xml
<!-- 當 M100 = 1 時觸發 Recipe1Selected 事件 -->
<Custom:PlcEventTrigger EventName="Recipe1Selected"
                        TriggerAddress="M100"
                        TriggerValue="1"/>

<!-- 當 D100 = 2 時觸發 Recipe2Selected 事件 -->
<Custom:PlcEventTrigger EventName="Recipe2Selected"
                        TriggerAddress="D100"
                        TriggerValue="2"/>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `EventName` | `string` | 事件名稱 | `""` |
| `TriggerAddress` | `string` | 觸發位址 | `""` |
| `TriggerValue` | `string` | 觸發值 | `""` |
| `TargetStatus` | `PlcStatus` | 指定 PLC | `null` (自動) |

#### 全域事件訂閱

```csharp
// 訂閱全域事件
PlcEventContext.EventTriggered += (sender, e) =>
{
    switch (e.EventName)
    {
        case "Recipe1Selected":
            LoadRecipe1();
            break;
            
        case "Recipe2Selected":
            LoadRecipe2();
            break;
            
        case "EmergencyStop":
            StopAllOperations();
            break;
    }
};
```

---

### 6. SensorViewer - 感測器監控面板

**用途：** 顯示多個感測器狀態，支援警報顯示、分組檢視。

#### 基本用法

```xml
<Custom:SensorViewer ConfigFilePath="Sensors.json"/>
```

#### Sensors.json 格式

```json
{
  "Sensors": [
    {
      "Device": "D90",
      "Group": "溫度監控",
      "OperationDescription": "加熱器溫度",
      "AlarmType": "HighLimit",
      "Threshold": 100.0
    },
    {
      "Device": "M100",
      "Group": "安全門檢測",
      "OperationDescription": "前門開關",
      "AlarmType": "BitOn",
      "Threshold": 1
    }
  ]
}
```

#### AlarmType 類型

| 類型 | 說明 | 觸發條件 |
|------|------|----------|
| `HighLimit` | 上限警報 | 數值 ? Threshold |
| `LowLimit` | 下限警報 | 數值 ? Threshold |
| `BitOn` | Bit ON | Bit = 1 |
| `BitOff` | Bit OFF | Bit = 0 |

#### 全域事件訂閱

```csharp
// 警報觸發
SensorContext.AlarmTriggered += (sender, e) =>
{
    var sensor = e.Sensor;
    var currentValue = e.Sensor.CurrentValue;
    
    // 根據感測器執行動作
    if (sensor.Device == "D90")
    {
        CyberMessageBox.Show(
            $"緊急警報！{sensor.OperationDescription}\n當前值：{currentValue}",
            "警報",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
};

// 警報消失
SensorContext.AlarmCleared += (sender, e) =>
{
    var duration = e.Duration.TotalSeconds;
    ComplianceContext.LogSystem(
        $"{e.Sensor.OperationDescription} 已恢復正常 (持續 {duration:F1} 秒)",
        LogLevel.Info
    );
};
```

---

### 7. LiveLogViewer - 即時日誌檢視器

**用途：** 顯示系統日誌和審計軌跡，支援自動滾動、顏色分類。

#### 基本用法

```xml
<Custom:LiveLogViewer Width="400" Height="300"/>
```

#### 日誌等級

| 等級 | 顏色 (Dark) | 顏色 (Light) | 用途 |
|------|-------------|--------------|------|
| `Success` | 綠色 | 綠色 | 成功操作 |
| `Info` | 藍色 | 藍色 | 一般資訊 |
| `Warning` | 橙色 | 橙色 | 警告訊息 |
| `Error` | 紅色 | 紅色 | 錯誤訊息 |

#### 程式碼記錄

```csharp
// 記錄系統日誌
ComplianceContext.LogSystem(
    "PLC 連線成功",
    LogLevel.Success,
    showInUi: true  // 顯示在 LiveLogViewer
);

// 記錄審計軌跡
ComplianceContext.LogAuditTrail(
    deviceName: "溫度設定",
    address: "D100",
    oldValue: "20",
    newValue: "30",
    reason: "製程調整",
    showInUi: true
);
```

#### 功能特色

- ? 自動滾動到最新日誌
- ? 最多保留 100 筆 (自動清除舊記錄)
- ? 時間戳格式：`HH:mm:ss.f`
- ? 支援 Dark/Light 主題

---

### 8. SecuredButton - 權限控制按鈕

**用途：** 根據使用者權限自動啟用/停用按鈕。

#### 基本用法

```xml
<!-- 需要 Operator 權限 -->
<Custom:SecuredButton Content="啟動製程" 
                     RequiredLevel="Operator"
                     Theme="Success"
                     Click="StartProcess_Click"/>

<!-- 需要 Engineer 權限 -->
<Custom:SecuredButton Content="修改參數" 
                     RequiredLevel="Engineer"
                     Theme="Warning"
                     Click="EditParameter_Click"/>

<!-- 需要 Supervisor 權限 -->
<Custom:SecuredButton Content="使用者管理" 
                     RequiredLevel="Supervisor"
                     Theme="Info"
                     Click="ManageUsers_Click"/>
```

#### 屬性

| 屬性 | 類型 | 說明 | 預設值 |
|------|------|------|--------|
| `Content` | `string` | 按鈕文字 | `""` |
| `RequiredLevel` | `AccessLevel` | 所需權限等級 | `Guest` |
| `Theme` | `ButtonTheme` | 按鈕主題 | `Primary` |

#### 按鈕主題

| Theme | 顏色 | 用途 |
|-------|------|------|
| `Primary` | 藍色 | 主要操作 |
| `Success` | 綠色 | 成功/啟動 |
| `Warning` | 橙色 | 警告/修改 |
| `Error` | 紅色 | 錯誤/停止 |
| `Info` | 青色 | 資訊/查詢 |

#### 權限等級

```csharp
public enum AccessLevel
{
    Guest = 0,       // 訪客（唯讀）
    Operator = 1,    // 操作員
    Instructor = 2,  // 指導員
    Supervisor = 3,  // 主管
    Engineer = 4     // 工程師（最高權限）
}
```

---

### 9. LoginDialog - 登入對話框

**用途：** 提供使用者登入介面，支援密碼驗證、記住帳號。

#### 基本用法

```csharp
// 顯示登入對話框
bool loginSuccess = LoginDialog.ShowLoginDialog();

if (loginSuccess)
{
    // 登入成功
    var user = SecurityContext.CurrentSession.CurrentUser;
}
else
{
    // 登入失敗或取消
    SecurityContext.QuickLogin(AccessLevel.Guest);
}
```

#### 預設測試帳號

| 帳號 | 密碼 | 權限等級 |
|------|------|----------|
| `admin` | `1234` | Engineer |
| `engineer` | `1234` | Engineer |
| `supervisor` | `1234` | Supervisor |
| `instructor` | `1234` | Instructor |
| `operator` | `1234` | Operator |

#### 功能特色

- ? 密碼加密 (SHA-256)
- ? 記住帳號功能
- ? 自動記錄到 Audit Trail
- ? 登入失敗記錄

---

### 10. CyberMessageBox - 訊息對話框

**用途：** 賽博風格的訊息對話框，支援多執行緒安全呼叫。

#### 基本用法

```csharp
// 簡單訊息
CyberMessageBox.Show(
    "操作成功！",
    "成功",
    MessageBoxButton.OK,
    MessageBoxImage.Information
);

// 確認對話框
var result = CyberMessageBox.Show(
    "確定要刪除嗎？",
    "確認",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question
);

if (result == MessageBoxResult.Yes)
{
    // 執行刪除
}

// 從背景執行緒呼叫（自動切換到 UI 執行緒）
Task.Run(() =>
{
    Thread.Sleep(1000);
    CyberMessageBox.Show("背景任務完成！", "完成");
});
```

#### 圖標類型

| 類型 | 顯示文字 | 顏色 |
|------|----------|------|
| `Information` | `[INFO]` | 藍色 |
| `Warning` | `[WARNING]` | 橙色 |
| `Error` | `[ERROR]` | 紅色 |
| `Question` | `[QUESTION]` | 青色 |
| `None` | `[OK]` | 白色 |

---

## ?? 核心系統

### 1. SecurityContext - 權限管理

**用途：** 統一管理使用者登入、權限檢查、自動登出。

#### 登入/登出

```csharp
// 標準登入
bool success = SecurityContext.Login("engineer", "1234");

// 快速登入（測試用）
SecurityContext.QuickLogin(AccessLevel.Engineer);

// 登出
SecurityContext.Logout();
```

#### 權限檢查

```csharp
// 檢查權限
if (SecurityContext.HasAccess(AccessLevel.Engineer))
{
    // 執行工程師功能
}

// 檢查權限並顯示錯誤訊息
if (SecurityContext.CheckAccess(AccessLevel.Supervisor, "使用者管理"))
{
    // 執行主管功能
}
```

#### 事件訂閱

```csharp
// 登入成功事件
SecurityContext.LoginSuccess += (sender, user) =>
{
    ComplianceContext.LogSystem(
        $"使用者 {user.DisplayName} 登入成功",
        LogLevel.Success
    );
};

// 登出事件
SecurityContext.LogoutOccurred += (sender, e) =>
{
    // 顯示登入對話框
    LoginDialog.ShowLoginDialog();
};

// 權限變更事件
SecurityContext.AccessLevelChanged += (sender, e) =>
{
    // 更新 UI 狀態
};
```

#### 自動登出設定

```csharp
// 啟用自動登出（預設 15 分鐘）
SecurityContext.EnableAutoLogout = true;
SecurityContext.AutoLogoutMinutes = 30;  // 改為 30 分鐘

// 更新活動時間（防止自動登出）
SecurityContext.UpdateActivity();
```

#### 取得當前使用者

```csharp
var session = SecurityContext.CurrentSession;

if (session.IsLoggedIn)
{
    var userName = session.CurrentUserName;       // "工程師"
    var level = session.CurrentLevel;             // AccessLevel.Engineer
    var loginTime = session.LoginTime;            // DateTime
    var user = session.CurrentUser;               // UserAccount
}
```

---

### 2. ComplianceContext - 法規合規引擎

**用途：** 符合 FDA 21 CFR Part 11 的電子記錄與審計軌跡管理。

#### 系統日誌

```csharp
// 記錄系統日誌（不寫入 SQLite）
ComplianceContext.LogSystem(
    "PLC 連線成功",
    LogLevel.Success,
    showInUi: true  // 顯示在 LiveLogViewer
);
```

#### 審計軌跡 (Audit Trail)

```csharp
// 記錄操作到 SQLite AuditTrails 表
ComplianceContext.LogAuditTrail(
    deviceName: "加熱器溫度",
    address: "D100",
    oldValue: "20",
    newValue: "30",
    reason: "製程調整",
    showInUi: true
);
```

#### 生產數據記錄 (Data History)

```csharp
// 記錄生產數據到 SQLite DataLogs 表
ComplianceContext.LogDataHistory(
    labelName: "溫度",
    address: "D100",
    value: "25.5"
);
```

#### 即時日誌存取

```csharp
// 取得即時日誌集合（綁定到 UI）
var logs = ComplianceContext.LiveLogs;

// 在 XAML 中綁定
LogList.ItemsSource = ComplianceContext.LiveLogs;
```

---

### 3. PlcContext - PLC 全域上下文

**用途：** 管理全域 PLC 實例，讓所有子元件自動連接。

#### 存取全域 PLC

```csharp
// 取得全域 PLC
var plcStatus = PlcContext.GlobalStatus;

if (plcStatus != null)
{
    var manager = plcStatus.CurrentManager;
    
    if (manager != null && manager.IsConnected)
    {
        // 讀取數據
        var data = await manager.ReadAsync("D100,10");
        
        // 寫入數據
        await manager.WriteAsync("M100,1");
    }
}
```

#### 設定區域 PLC

```xml
<!-- 在父容器設定 PLC -->
<StackPanel local:PlcContext.Status="{Binding ElementName=MainPlc}">
    <!-- 這些 PlcLabel 會自動使用 MainPlc -->
    <Custom:PlcLabel Label="溫度" Address="D100"/>
    <Custom:PlcLabel Label="壓力" Address="D101"/>
</StackPanel>
```

---

### 4. SensorContext - 感測器管理

**用途：** 管理感測器配置、警報觸發。

#### 載入配置

```csharp
// 從 JSON 檔案載入
SensorContext.LoadFromFile("Sensors.json");

// 手動加入感測器
SensorContext.AddSensor(new SensorConfig
{
    Device = "D90",
    Group = "溫度監控",
    OperationDescription = "加熱器溫度",
    AlarmType = AlarmType.HighLimit,
    Threshold = 100.0
});
```

#### 事件訂閱

```csharp
// 警報觸發
SensorContext.AlarmTriggered += (sender, e) =>
{
    var sensor = e.Sensor;
    var eventTime = e.EventTime;
    
    // 播放警報音效
    SystemSounds.Beep.Play();
    
    // 顯示訊息
    CyberMessageBox.Show(
        $"警報：{sensor.OperationDescription}",
        "警報",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
};

// 警報消失
SensorContext.AlarmCleared += (sender, e) =>
{
    var duration = e.Duration;
    
    ComplianceContext.LogSystem(
        $"{e.Sensor.OperationDescription} 已恢復正常",
        LogLevel.Info
    );
};
```

---

## ?? 主題系統

### 切換主題

#### 方式 1：透過 CyberFrame 按鈕

點擊 CyberFrame 右上角的月亮圖標即可切換主題。

#### 方式 2：程式碼切換

```csharp
// 在 CyberFrame.xaml.cs 中
private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
{
    var app = Application.Current;
    var dictionaries = app.Resources.MergedDictionaries;
    
    // 移除舊主題
    var oldTheme = dictionaries.FirstOrDefault(d => 
        d.Source?.OriginalString.Contains("Colors.xaml") == true ||
        d.Source?.OriginalString.Contains("LightColors.xaml") == true);
    
    if (oldTheme != null)
        dictionaries.Remove(oldTheme);
    
    // 載入新主題
    var newTheme = new ResourceDictionary();
    newTheme.Source = new Uri(
        "/Stackdose.UI.Core;component/Themes/LightColors.xaml", 
        UriKind.Relative
    );
    dictionaries.Add(newTheme);
}
```

### 自訂顏色

修改 `Colors.xaml` (Dark) 或 `LightColors.xaml` (Light)：

```xml
<!-- 修改主要強調色 -->
<SolidColorBrush x:Key="Cyber.NeonBlue" Color="#00E5FF"/>

<!-- 修改背景色 -->
<SolidColorBrush x:Key="Cyber.Bg.Dark" Color="#0F0F1A"/>

<!-- 修改文字色 -->
<SolidColorBrush x:Key="Cyber.Text.Main" Color="#FFFFFF"/>
```

---

## ??? 資料庫結構

### 資料庫位置

```
YourApp\bin\Debug\net8.0-windows\StackDoseData.db
```

### 資料表結構

#### 1. DataLogs（生產數據履歷）

```sql
CREATE TABLE DataLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    LabelName TEXT,      -- PlcLabel 名稱
    Address TEXT,        -- PLC 位址
    Value TEXT           -- 數值
);
```

**範例資料：**

| Id | Timestamp | LabelName | Address | Value |
|----|-----------|-----------|---------|-------|
| 1 | 2024-01-15 10:23:45 | 溫度 | D100 | 25.5 |
| 2 | 2024-01-15 10:23:50 | 壓力 | D200 | 10.2 |

#### 2. AuditTrails（審計軌跡）

```sql
CREATE TABLE AuditTrails (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    User TEXT,           -- 使用者帳號
    Action TEXT,         -- 動作 (WRITE, LOGIN, LOGOUT)
    TargetDevice TEXT,   -- 目標裝置
    OldValue TEXT,       -- 修改前的值
    NewValue TEXT,       -- 修改後的值
    Reason TEXT          -- 原因
);
```

**範例資料：**

| Id | Timestamp | User | Action | TargetDevice | OldValue | NewValue | Reason |
|----|-----------|------|--------|--------------|----------|----------|--------|
| 1 | 2024-01-15 10:20:00 | engineer | LOGIN | N/A | Logged Out | Logged In | Login from DESKTOP-ABC |
| 2 | 2024-01-15 10:25:00 | engineer | WRITE | 溫度設定(D100) | 20 | 30 | 製程調整 |

### 查詢範例

```sql
-- 查詢最近 10 筆生產數據
SELECT * FROM DataLogs 
ORDER BY Timestamp DESC 
LIMIT 10;

-- 查詢特定使用者的操作記錄
SELECT * FROM AuditTrails 
WHERE User = 'engineer' 
ORDER BY Timestamp DESC;

-- 查詢特定裝置的變更歷史
SELECT * FROM AuditTrails 
WHERE TargetDevice LIKE '%D100%' 
ORDER BY Timestamp DESC;
```

---

## ?? 最佳實踐

### 1. 權限管理

```csharp
// ? 推薦：在 MainWindow 建構子中初始化
public MainWindow()
{
    InitializeComponent();
    
    // 測試環境：快速登入
    SecurityContext.QuickLogin(AccessLevel.Engineer);
    
    // 正式環境：顯示登入對話框
    // bool loginSuccess = LoginDialog.ShowLoginDialog();
    // if (!loginSuccess)
    // {
    //     Application.Current.Shutdown();
    // }
}
```

### 2. PLC 連線管理

```csharp
// ? 推薦：使用全域 PLC + 事件訂閱
MainPlc.ConnectionEstablished += (manager) =>
{
    // PLC 連線成功後的初始化
    InitializePlcDevices();
};

// ? 避免：在沒有連線檢查的情況下直接存取
var manager = PlcContext.GlobalStatus?.CurrentManager;
if (manager != null && manager.IsConnected)  // ← 必須檢查
{
    await manager.WriteAsync("M100,1");
}
```

### 3. 審計軌跡記錄

```csharp
// ? 推薦：所有寫入操作都記錄
private async Task WriteToPlc(string address, string value, string reason)
{
    var manager = PlcContext.GlobalStatus?.CurrentManager;
    if (manager == null || !manager.IsConnected)
        return;
    
    // 讀取舊值
    var oldData = await manager.ReadAsync(address);
    var oldValue = oldData?.FirstOrDefault()?.ToString() ?? "Unknown";
    
    // 寫入新值
    await manager.WriteAsync($"{address},{value}");
    
    // 記錄到 Audit Trail
    ComplianceContext.LogAuditTrail(
        deviceName: "溫度設定",
        address: address,
        oldValue: oldValue,
        newValue: value,
        reason: reason,
        showInUi: true
    );
}
```

### 4. 多 PLC 場景

```xml
<!-- ? 推薦：明確命名和配置 -->
<Custom:PlcStatus x:Name="MainPlc" 
                 IpAddress="192.168.1.1" 
                 Port="3000" 
                 IsGlobal="True"/>

<Custom:PlcStatus x:Name="SubPlc" 
                 IpAddress="192.168.1.2" 
                 Port="3000" 
                 IsGlobal="False"/>

<!-- 明確指定 TargetStatus -->
<Custom:PlcLabel Label="主機溫度" 
                Address="D100" 
                TargetStatus="{Binding ElementName=MainPlc}"/>

<Custom:PlcLabel Label="副機溫度" 
                Address="D100" 
                TargetStatus="{Binding ElementName=SubPlc}"/>
```

### 5. 錯誤處理

```csharp
// ? 推薦：完整的錯誤處理
try
{
    await MainPlc.ConnectAsync();
}
catch (Exception ex)
{
    ComplianceContext.LogSystem(
        $"PLC 連線失敗：{ex.Message}",
        LogLevel.Error,
        showInUi: true
    );
    
    CyberMessageBox.Show(
        $"PLC 連線失敗\n\n錯誤：{ex.Message}",
        "錯誤",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
}
```

---

## ? 常見問題

### 1. PlcLabel 沒有顯示數值？

**檢查清單：**
- ? PlcStatus 是否已連線？（查看連線指示燈）
- ? Address 格式是否正確？（如 `D100`）
- ? 是否設定了 `TargetStatus`？（多 PLC 場景）
- ? PLC 是否有回應？（使用 PlcDeviceEditor 測試）

### 2. PlcDeviceEditor 被鎖定 [LOCK]？

**原因：** 當前使用者權限不足（需要 Engineer 權限）

**解決方案：**
```csharp
// 切換為 Engineer 權限
SecurityContext.QuickLogin(AccessLevel.Engineer);
```

### 3. 主題切換後元件變成白色？

**原因：** App.xaml 中沒有載入主題資源

**解決方案：**
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- 必須載入主題 -->
            <ResourceDictionary Source="/Stackdose.UI.Core;component/Themes/Colors.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 4. CyberFrame 沒有自動填滿視窗？

**解決方案：**
```xml
<!-- 方式 1：不設定寬高（推薦）-->
<Custom:CyberFrame/>

<!-- 方式 2：明確設定 Stretch -->
<Custom:CyberFrame HorizontalAlignment="Stretch" 
                  VerticalAlignment="Stretch"/>
```

### 5. 如何查看資料庫記錄？

**工具：** DB Browser for SQLite

**資料庫位置：**
```
YourApp\bin\Debug\net8.0-windows\StackDoseData.db
```

### 6. LiveLogViewer 在 Light 模式下看不清楚？

**已修正：** 最新版本已將 Light 模式的背景色從純白 (#FFFFFF) 改為淺灰 (#F5F5F5)

### 7. 如何停用自動登出？

```csharp
// 在 MainWindow 建構子中
SecurityContext.EnableAutoLogout = false;
```

### 8. 如何自訂權限檢查邏輯？

```csharp
// 自訂權限檢查
public bool HasCustomAccess(string operation)
{
    var level = SecurityContext.CurrentSession.CurrentLevel;
    
    // 自訂邏輯
    return operation switch
    {
        "StartProcess" => level >= AccessLevel.Operator,
        "EditParameter" => level >= AccessLevel.Engineer,
        "ManageUser" => level >= AccessLevel.Supervisor,
        _ => false
    };
}
```

---

## ?? 技術支援

- **GitHub Issues**: https://github.com/17app001/Stackdose.UI.Core/issues
- **Email**: support@stackdose.com
- **文件更新**: 2024-01-15

---

## ?? 授權

本專案採用 MIT 授權。詳見 LICENSE 檔案。

---

## ?? 版本歷史

### v1.0.0 (2024-01-15)
- ? 初始版本發布
- ? 完整的 PLC 控制元件
- ? 權限管理系統
- ? 法規合規引擎
- ? Dark/Light 主題支援
- ? SQLite 審計軌跡記錄

---

**感謝使用 Stackdose.UI.Core！** ??
