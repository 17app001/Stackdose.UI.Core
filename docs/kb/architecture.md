# 架構設計知識庫

> 記錄 Stackdose.UI.Core 核心架構設計決策、Pattern 與資料流。

---

## 1. 核心架構哲學

框架採**靜態 Context Manager Pattern**，刻意不用 DI 容器。
理由：工業設備 UI 的全域狀態（PLC、使用者登入、日誌）需要跨控制項樹隨時存取，DI 在 WPF Code-behind 中注入成本高。靜態存取犧牲可測試性換取開發速度，符合此類專案的實際需求。

---

## 2. Static Context 系統

### 2.1 PlcContext
- **職責：** 全域 PLC 實例管理、附加屬性傳遞給子控制項
- **位置：** `Stackdose.UI.Core/Helpers/PlcContext.cs`
- **關鍵屬性：**
  - `GlobalStatus` — 全域 PLC 狀態（`IPlcManager`）
  - `PlcManager` Attached Property — WPF 附加屬性，讓子控制項無需知道父層即可取得 Manager
- **資料流：** App 啟動時設定 `PlcContext.GlobalStatus`，`PlcLabel` 等控制項訂閱 `ScanUpdated` 事件自動更新

### 2.2 SecurityContext
- **職責：** 使用者登入/登出、權限檢查、自動登出計時器
- **位置：** `Stackdose.UI.Core/Helpers/SecurityContext.cs`
- **存取等級：** `Guest < Operator < Instructor < Supervisor < Admin < SuperAdmin`
- **關鍵方法：**
  - `Login(username, password)` — AD 或本機 SQLite 驗證
  - `CheckAccess(requiredLevel)` — 檢查當前使用者是否有足夠權限
  - `StartAutoLogoutTimer()` — 閒置自動登出

### 2.3 ComplianceContext
- **職責：** 統一 FDA 21 CFR Part 11 合規日誌 API
- **位置：** `Stackdose.UI.Core/Helpers/ComplianceContext.cs`
- **關鍵方法：**
  - `LogAuditTrail(action, detail)` — 稽核軌跡（強制記錄）
  - `LogOperation(action, detail)` — 操作記錄
  - `LogEvent(eventName, detail)` — 系統事件
  - `LogPeriodicData(data)` — 週期性製程數據
  - `Shutdown()` — **必須呼叫**，確保佇列資料落盤
- **底層：** `SqliteLogger`（非同步批次，5秒 flush）

### 2.4 SensorContext
- **職責：** 感測器配置載入、警報狀態追蹤
- **位置：** `Stackdose.UI.Core/Helpers/SensorContext.cs`

### 2.5 ThemeManager
- **職責：** 動態主題切換（Dark/Light）、WeakReference 控制項登錄
- **位置：** `Stackdose.UI.Core/Services/ThemeManager.cs`
- **主題入口：** `Themes/Theme.xaml`
- **語意 Token 規則：** `Surface.*`（背景）、`Text.*`（文字）、`Action.*`（按鈕/操作）

---

## 3. JSON 驅動配置系統

### 設定檔結構
```
Config/
├── app-meta.json          ← Header 標題、導航項目、Hot Reload 策略
└── Machine*.config.json   ← 每台設備：PLC IP/Port、Tags、Commands、Alarm/Sensor
```

### 執行流程
```
AppController.Start()
  → BootstrapService.Start()
    → RuntimeHost.Start()
      → 掃描 Config/ 目錄
      → 載入 app-meta.json + Machine*.config.json
      → DeviceContextMapper.CreateDeviceContext()
        → MachineConfig → DeviceContext（含 Labels、Commands、Modules）
      → DynamicDevicePage 自動呈現
```

### 擴充點（優先順序）
1. 純 JSON + `DynamicDevicePage`（最快，零程式碼）
2. 覆寫 `IRuntimeMappingAdapter` / `DefaultRuntimeMappingAdapter`
3. 覆寫 `RuntimeMapper.CreateDeviceContext(...)`
4. `AppController.ConfigurePageFactory(...)` 換自訂 Device Page
5. `AppController.SettingsPage` 客製 Settings

---

## 4. Shell 導航系統

```
ShellRouteCatalog
  → Overview / Detail / Log / User / Settings
NavigationOrchestrator — 統一頁面切換
IShellAppProfile — 定義 App metadata（標題、圖示、導航項目）
ShellNavigationService — 導航契約介面（多 App 複用）
```

---

## 5. 資料流

### PLC 輪詢
```
IPlcMonitor.Start()
  → 定期 BatchRead（可設定 intervalMs）
  → WordChanged / BitChanged 事件
  → PlcLabel / PlcStatus 控制項訂閱更新
```

### 命令執行
```
SecuredButton（權限檢查 SecurityContext.CheckAccess）
  → ProcessCommandService.ExecuteAsync(command)
  → IPlcManager.WriteAsync(address, value)
  → ComplianceContext.LogAuditTrail(...)
```

### 日誌寫入
```
ComplianceContext.LogXxx(...)
  → SqliteLogger.EnqueueAsync(entry)
  → 批次佇列（5秒 flush，或 Shutdown 時立即 flush）
  → SQLite DB（合規資料庫）
  → LiveLogViewer 即時顯示（透過 ObservableCollection）
```

---

## 6. 控制項基類體系

```
CyberControlBase
  └── PlcControlBase
        ├── PlcLabel
        ├── PlcText
        ├── PlcStatus
        └── PlcStatusIndicator
```

- `CyberControlBase`：主題、WeakReference 登錄
- `PlcControlBase`：PLC 附加屬性綁定、`ScanUpdated` 訂閱

---

## 7. 已知技術債

| 問題 | 位置 | 影響 |
|---|---|---|
| `DynamicDevicePage.xaml` 硬編碼色碼 | DeviceFramework/Pages/ | 不符主題 Token 規範 |
| ~~`DeviceFramework-Guide.md` 亂碼~~ | —— | 已由 `docs/kb/deviceframework-guide.md` 取代，新人請讀該文件 |
| UbiDemo 部分架構文件亂碼 | App.UbiDemo/ | 已遷移至 DeviceFramework，影響有限 |
