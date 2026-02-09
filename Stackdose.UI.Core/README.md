# Stackdose.UI.Core

Stackdose.UI.Core 是工業場景用的 WPF 核心 UI 函式庫，重點在 PLC 互動、權限管理、合規日誌與主題資源。

- Framework: `net8.0-windows`
- UI: `WPF`
- Data: `SQLite` + `Dapper`

---

## 快速開始

### 1) 參考專案

```xml
<ItemGroup>
  <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
</ItemGroup>
```

### 2) 在 `App.xaml` 合併主題資源

```xml
<Application x:Class="YourApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Stackdose.UI.Core;component/Themes/Theme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### 3) 初始化安全與合規上下文

```csharp
using Stackdose.UI.Core.Helpers;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SecurityContext.EnableAutoLogout = true;
        SecurityContext.AutoLogoutMinutes = 30;

        _ = new Stackdose.UI.Core.Services.UserManagementService();
        _ = ComplianceContext.CurrentUser;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ComplianceContext.Shutdown();
        base.OnExit(e);
    }
}
```

---

## 專案結構

- `Controls/`: WPF 控制項（如 `PlcStatus`、`PlcLabel`、`LoginDialog`、`SecuredButton`）
- `Helpers/`: 核心上下文與管理器（`SecurityContext`、`ComplianceContext`、`SqliteLogger`、`PlcContext`）
- `Services/`: 使用者與驗證服務（`UserManagementService`、`AdAuthenticationService`）
- `Models/`: 列舉與資料模型（`AccessLevel`、`UserAccount`、`UserSession`、`LogEntry`）
- `Themes/`: 主題字典與控制項樣式資源

---

## 核心功能

### 安全控制（`Helpers/SecurityContext.cs`）

- 提供登入 / 登出 API
- 權限檢查：`HasAccess` / `CheckAccess`
- 支援自動登出計時
- 維護 Session 狀態與事件（`LoginSuccess`、`LogoutOccurred`、`AccessLevelChanged`）

### 合規日誌（`Helpers/ComplianceContext.cs`）

- 統一記錄 API：
  - `LogSystem`
  - `LogAuditTrail`
  - `LogOperation`
  - `LogEvent`
  - `LogPeriodicData`
- 即時 UI 日誌集合：`LiveLogs`

### SQLite 批次記錄（`Helpers/SqliteLogger.cs`）

- 佇列式批次寫入
- 定時刷新（預設 5 秒）
- 支援手動 flush 與關閉前完整落盤

### PLC 上下文（`Helpers/PlcContext.cs`）

- 全域 PLC 狀態（`GlobalStatus`）
- 以附加屬性讓子控制項繼承 PLC 狀態（`StatusProperty`）

---

## 權限等級

定義於 `Models/AccessLevel.cs`：

- `Guest` (0)
- `Operator` (1)
- `Instructor` (2)
- `Supervisor` (3)
- `Admin` (4)
- `SuperAdmin` (5)

---

## 主題系統

- 入口：`Themes/Theme.xaml`
- 預設載入：`Themes/Colors.xaml`
- 可切換淺色：`Themes/LightColors.xaml`

---

## 外部依賴

來源：`Stackdose.UI.Core.csproj`

- NuGet 套件：
  - `Dapper`
  - `Microsoft.Data.Sqlite`
  - `System.DirectoryServices.AccountManagement`
- Project references outside this repo subtree:
  - `..\..\Sdk\FeiyangWrapper\FeiyangWrapper\FeiyangWrapper.vcxproj`
  - `..\..\Stackdose.Platform\Stackdose.Core\Stackdose.Core.csproj`
  - `..\..\Stackdose.Platform\Stackdose.Hardware\Stackdose.Hardware.csproj`
  - `..\..\Stackdose.Platform\Stackdose.PrintHead\Stackdose.PrintHead.csproj`

若上述相鄰專案不存在，完整編譯會失敗。

---

## 整合檢查清單

1. 在 App 資源合併 `Theme.xaml`。
2. 啟動時初始化 `UserManagementService`。
3. 關鍵操作統一走 `ComplianceContext` API。
4. 結束前呼叫 `ComplianceContext.Shutdown()`。
5. 權限敏感按鈕使用 `SecuredButton` + `SecurityContext.CheckAccess(...)` 雙層保護。
