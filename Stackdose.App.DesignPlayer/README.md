# Stackdose.App.DesignPlayer

> 可交付量產的 WPF Shell 應用程式 — 讀取 `.machinedesign.json` 並連接真實 PLC

## 定位

`DesignPlayer` 是設計器工作流程的最終交付端點：

```
MachinePageDesigner（設計器）
    ↓ 輸出 .machinedesign.json
DesignViewer（靜態預覽）
    ↓ 驗證外觀
DesignPlayer（量產交付）← 本專案
    ↓ 給設備廠商部署到現場
```

與 `DesignRuntime`（開發者測試工具）不同，`DesignPlayer` 具備完整的量產 Shell UI：
- 左側導航欄、頁首設備名稱、頁尾狀態列
- 可選登入對話框（`loginRequired: true`）
- 使用者管理頁面（AccessLevel 管控）
- 外部 JSON 設定，無需修改程式碼即可調整

## 設定檔

所有設定放在 `Config/` 目錄，部署時隨可執行檔一起發布。

### `Config/app-config.json`

```json
{
  "appTitle": "Stackdose Monitor",
  "headerDeviceName": "MONITOR",
  "loginRequired": false,
  "plc": {
    "ip": "192.168.1.100",
    "port": 3000,
    "pollIntervalMs": 500,
    "autoConnect": true
  },
  "designFile": "Config/monitor.machinedesign.json"
}
```

| 欄位 | 說明 |
|---|---|
| `appTitle` | 視窗標題列與頁首顯示名稱 |
| `headerDeviceName` | 左上角設備識別碼（大寫） |
| `loginRequired` | `true` 時啟動自動出現登入對話框 |
| `plc.ip` | PLC IP 位址 |
| `plc.port` | PLC 連線埠（預設 3000） |
| `plc.pollIntervalMs` | 輪詢間隔毫秒（建議 500）|
| `plc.autoConnect` | 啟動時自動連線 |
| `designFile` | 畫布設計檔案相對路徑 |

### `Config/monitor.machinedesign.json`

由 `MachinePageDesigner` 輸出的畫布設計檔，格式如下：

```json
{
  "version": "1.0",
  "canvasWidth": 1100,
  "canvasHeight": 200,
  "canvasItems": [
    {
      "type": "PlcLabel",
      "x": 10, "y": 10,
      "width": 200, "height": 160,
      "properties": {
        "label": "壓差",
        "address": "D103",
        "valueColorTheme": "NeonBlue",
        "frameShape": "Rectangle"
      }
    }
  ]
}
```

## 支援的控制項類型

| type | 控制項 | 說明 |
|---|---|---|
| `PlcLabel` | PlcLabel | 數值顯示（含標題、單位、顏色主題） |
| `PlcText` | PlcText | 文字顯示 |
| `PlcStatusIndicator` | PlcStatusIndicator | 位元狀態燈 |
| `SecuredButton` | SecuredButton | 需授權的 PLC 寫入按鈕 |
| `Spacer` | GroupBox | 標題分組框 |
| `LiveLog` | LiveLog | 即時 PLC 事件日誌 |
| `AlarmViewer` | AlarmViewer | 警報記錄查閱 |
| `SensorViewer` | SensorViewer | 感測器歷史記錄 |

## Shell 導航

應用程式預設包含兩個頁面，以左側導航欄切換：

| 頁面 | NavigationTarget | 所需權限 |
|---|---|---|
| Main View | `monitor` | Guest（任何人） |
| User Management | `users` | Operator 以上 |

## 專案結構

```
Stackdose.App.DesignPlayer/
├── Config/
│   ├── app-config.json           ← 應用程式設定
│   └── monitor.machinedesign.json← 畫布設計範例
├── Models/
│   └── PlayerAppConfig.cs        ← 設定 Model
├── Services/
│   └── PlayerConfigLoader.cs     ← JSON 載入服務
├── Pages/
│   ├── MonitorPage.xaml          ← 主監控頁（Canvas 渲染）
│   └── MonitorPage.xaml.cs
├── RuntimeControlFactory.cs      ← 依 type 建立 live 控制項
├── App.xaml / App.xaml.cs        ← 啟動 + 主題初始化
└── MainWindow.xaml / .xaml.cs    ← Shell 組裝 + 導航
```

## 與其他專案的差異

| 特性 | DesignRuntime | DesignPlayer |
|---|---|---|
| 目標 | 開發驗證 | 量產部署 |
| Shell UI | 無（裸視窗） | 完整（導航 + 頁首 + 頁尾） |
| 登入管控 | 無 | 可選（JSON 設定） |
| 用途 | 設計時快速測試 PLC | 交付給設備廠商使用 |
| 使用者管理 | 無 | 有 |

## 依賴

- `Stackdose.UI.Core` — 26 個 WPF 控制項、Context 系統
- `Stackdose.UI.Templates` — Shell 布局（MainContainer）
- `Stackdose.App.DeviceFramework` — DesignFileService、DesignDocument
- `Stackdose.Tools.MachinePageDesigner` — DesignerItemDefinition 型別
- `Stackdose.Platform/Stackdose.Hardware` — PLC 連線實作
