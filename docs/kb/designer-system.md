# 設計器系統知識庫

> 涵蓋 MachinePageDesigner、DesignViewer、DesignRuntime、DesignPlayer 四個工具的架構與使用方式。

---

## 1. 設計器系統概覽

四個專案構成完整的「設計 → 預覽 → 驗證 → 量產」工作流程：

```
MachinePageDesigner   →→→  DesignViewer    →→→  DesignRuntime  →→→  DesignPlayer
（拖曳設計畫布）            （JSON靜態預覽）       （開發者驗證用）       （量產部署）
輸出 .machinedesign.json   拖入JSON即時渲染      載入JSON + 連線PLC   完整Shell UI + 登入管控
```

---

## 2. MachinePageDesigner

**專案：** `Stackdose.Tools.MachinePageDesigner`
**狀態：** 主力開發（自由畫布模式已完成）

### 2.1 設計哲學演進

| 版本 | 模式 | 說明 |
|---|---|---|
| v1.0（2026-04-01） | Zone 制（Grid排列） | 固定功能區塊，UniformGrid 自動排列 |
| v2.0（2026-04-08+） | **自由畫布（FreeCanvas）** | 拖曳任意位置，完全自由定位 |

目前為 v2.0 自由畫布模式。

### 2.2 自由畫布（FreeCanvas）功能清單

| 功能 | 狀態 | 說明 |
|---|---|---|
| 拖曳任意放置 | ✅ | 控制項可拖到畫布任意位置 |
| Snap 對齊 | ✅ | 拖曳時自動吸附格線 |
| Smart Snap | ✅ | 磁吸對齊其他控制項邊緣/中心 |
| Z-Order 控制 | ✅ | 前置/後置層級調整 |
| 框選（多選） | ✅ | 拖曳框選多個控制項 |
| 右鍵 ContextMenu | ✅ | 複製/貼上/刪除/鎖定/Z-Order 快速選單 |
| 鎖定 | ✅ | 鎖定控制項防止誤移 |
| 複製貼上 | ✅ | Ctrl+C / Ctrl+V |
| 多選同步 | ✅ | 多選同步移動 |
| GroupBox | ✅ | 將多個控制項組合成群組 |
| 對齊分配 | ✅ | 左對齊、水平均等分配等 |
| 畫布尺寸設定 | ✅ | 可自訂畫布寬高 |
| Undo/Redo | ✅ | Ctrl+Z / Ctrl+Y（每頁獨立） |
| 儲存/載入 | ✅ | .machinedesign.json（v2.0 多頁格式） |
| 9 種控制項全支援 | ✅ | 含 StaticLabel，每種均有屬性面板 |
| **多頁面支援** | ✅ | 頁籤列切換頁面，每頁獨立畫布尺寸與 Undo stack |

### 2.3 輸出格式（.machinedesign.json，v2.0）

```json
{
  "version": "2.0",
  "meta": {
    "title": "頁面標題",
    "machineId": "M1",
    "createdAt": "2026-04-16T00:00:00Z",
    "modifiedAt": "2026-04-16T00:00:00Z"
  },
  "pages": [
    {
      "pageId": "a1b2c3d4",
      "name": "Main",
      "canvasWidth": 1280,
      "canvasHeight": 720,
      "canvasItems": [
        {
          "type": "PlcLabel",
          "x": 100, "y": 200,
          "width": 120, "height": 80,
          "props": {
            "label": "溫度",
            "address": "D100",
            "valueColorTheme": "NeonBlue",
            "frameShape": "Rectangle"
          }
        }
      ]
    },
    {
      "pageId": "e5f6g7h8",
      "name": "Alarms",
      "canvasWidth": 1280,
      "canvasHeight": 720,
      "canvasItems": []
    }
  ],
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "canvasItems": [...]
}
```

> **注意（v2.0 格式）：**
> - 主要內容在 `pages` 陣列，每頁有獨立的 `canvasWidth/Height` 與 `canvasItems`
> - 頂層 `canvasItems / canvasWidth / canvasHeight` 為向後相容欄位（等同 `pages[0]`），DesignRuntime / DesignPlayer 讀取此欄位
> - v1.0 舊檔（無 `pages` 鍵）載入時由 `DesignFileService` 自動轉為單頁 "Main"

### 2.4 支援控制項（9 種）

| 控制項 | 分類 | 說明 |
|---|---|---|
| `PlcLabel` | PLC | PLC 數值顯示，色彩主題 + 外框形狀 + 除數換算 |
| `PlcText` | PLC | PLC 文字顯示 |
| `PlcStatusIndicator` | PLC | 位元狀態指示燈（M/D 位址） |
| `SecuredButton` | Button | 需權限驗證的操作按鈕 |
| `StaticLabel` | Layout | 靜態文字（標題、說明），可設字體大小/粗細/對齊/顏色 |
| `Spacer` | Layout | GroupBox 群組化框 |
| `LiveLog` | Viewer | 即時系統日誌面板（無需設定） |
| `AlarmViewer` | Viewer | 警報列表（`configFile` 指向 alarms.json） |
| `SensorViewer` | Viewer | 感測器數值列表（`configFile` 指向 sensors.json） |

### 2.5 快捷鍵

| 快捷鍵 | 功能 |
|---|---|
| `Ctrl+N` | 新建 |
| `Ctrl+O` | 開啟 |
| `Ctrl+S` | 儲存 |
| `Ctrl+Shift+S` | 另存新檔 |
| `Ctrl+Z` | 復原 |
| `Ctrl+Y` | 重做 |
| `Ctrl+C` / `Ctrl+V` | 複製/貼上 |
| `Delete` | 刪除選取 |
| `Shift+Click` | 多選 |
| 右鍵 | ContextMenu（複製/貼上/刪除/鎖定/Z-Order） |

### 2.6 專案依賴
```
Stackdose.Tools.MachinePageDesigner
├── → Stackdose.UI.Core（PlcLabel 等真實控制項，WYSIWYG）
└── → Stackdose.App.DeviceFramework（DeviceLabelViewModel、PlcDataGridPanel）
```

---

## 3. DesignViewer

**專案：** `Stackdose.Tools.DesignViewer`
**狀態：** ✅ 功能完整

### 3.1 用途
純預覽工具，不連 PLC，不需安裝完整執行環境。使用者拖入 `.machinedesign.json` 即可即時渲染頁面外觀。

### 3.2 使用方式
1. 執行 `Stackdose.Tools.DesignViewer.exe`
2. 將 `.machinedesign.json` 拖入視窗
3. 畫布即時渲染（控制項顯示預設值/模擬值）

### 3.3 專案依賴
```
Stackdose.Tools.DesignViewer
├── → Stackdose.UI.Core
└── → Stackdose.Tools.MachinePageDesigner（共用渲染邏輯）
```

---

## 4. DesignRuntime

**專案：** `Stackdose.App.DesignRuntime`
**狀態：** ✅ 功能完整

### 4.1 用途
開發者驗證工具：手動輸入 PLC IP、開啟 JSON，確認控制項顯示正確的實際 PLC 數值。適合設計完成後上線前的最終驗證，不適合量產部署。

### 4.2 主要功能

| 功能 | 說明 |
|---|---|
| 手動輸入 PLC IP / Port / Scan | 工具列直接輸入，無需設定檔 |
| 模擬器模式 | 勾選後無需真實 PLC 即可連線 |
| 亂數測試注入 | 自動寫入 D100~D102 隨機值，驗證數值更新 |
| JSON 熱更新 | 偵測到 .machinedesign.json 變更後自動重載畫布，保留當前頁 |
| 縮放 | 0.25x ~ 2.0x 滑桿縮放 |
| **PLC 斷線重連** | 看門狗偵測斷線 → UI 顯示 `⚠ 重連中` → 自動重新連線 → `RefreshMonitors()` |
| **多頁面切換** | 讀取 `pages` 陣列，頂部頁籤列切換；單頁自動隱藏頁籤列 |

### 4.3 架構重點（`MainWindow.xaml.cs`）
- `RenderDocument(doc, path)` → `BuildPageTabs` + `SwitchPage(index)`
- `SwitchPage(index)` → 更新頁籤高亮 → `RenderPage(DesignPage)`
- `RenderPage` 讀 `page.CanvasItems`，渲染後呼叫 `RefreshMonitors()`

### 4.3 專案依賴
```
Stackdose.App.DesignRuntime
├── → Stackdose.UI.Core
├── → Stackdose.App.DeviceFramework（ProcessCommandService）
└── → Stackdose.Tools.MachinePageDesigner（DesignFileService、DesignDocument）
```

---

## 5. DesignPlayer

**專案：** `Stackdose.App.DesignPlayer`
**狀態：** 開發中（多頁面切換進行中）

### 5.1 用途
可交付量產的 Shell App。設備廠商直接部署到現場，不需修改程式碼，只需編輯 `Config/` 下的 JSON 設定。

### 5.2 與 DesignRuntime 的差異

| 特性 | DesignRuntime | DesignPlayer |
|---|---|---|
| 目標 | 開發者驗證 | 量產部署 |
| Shell UI | 無（裸視窗） | 完整（左側導航 + 頁首 + 頁尾） |
| PLC 設定方式 | 工具列手動輸入 | `app-config.json` 或 Settings 頁面（GUI） |
| 登入管控 | 無 | 可選（`loginRequired`） |
| 使用者管理 | 無 | 內建（Operator 以上） |
| Settings 頁面 | 無 | ✅（免 JSON 修改 PLC / 設計稿路徑） |
| 開啟 JSON | 手動選擇或拖曳 | 啟動時自動載入 `designFile` 路徑 |
| JSON 熱更新 | ✅ | ✅ |

### 5.3 設定檔

**`Config/app-config.json`**

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

### 5.4 完整比較表

| | MachinePageDesigner | DesignViewer | DesignRuntime | DesignPlayer |
|---|---|---|---|---|
| PLC連線 | ❌ | ❌ | ✅ | ✅ |
| 即時數值 | ❌ | ❌ | ✅ | ✅ |
| 編輯功能 | ✅ | ❌ | ❌ | ❌ |
| Shell UI | ❌ | ❌ | ❌ | ✅ |
| 登入管控 | ❌ | ❌ | ❌ | 可選 |
| JSON 熱更新 | — | — | ✅ | ✅ |
| 多頁面 | ✅ | ✅ | ✅ | ✅ |
| 用途 | 設計 | 預覽 | 開發驗證 | 量產交付 |

### 5.5 專案依賴
```
Stackdose.App.DesignPlayer
├── → Stackdose.UI.Core（控制項 + Context）
├── → Stackdose.UI.Templates（MainContainer Shell）
├── → Stackdose.App.DeviceFramework（DesignFileService、ProcessCommandService）
└── → Stackdose.Tools.MachinePageDesigner（DesignDocument、DesignerItemDefinition）
```

---

## 6. Dashboard 模式

**加入版本：** 2026-04-20

### 6.1 用途

Dashboard 模式適合「全屏無人操作的監控看板」場景：工廠展示螢幕、單機台狀態牆、無須操作員登入的儀表板。

**與標準模式的差異：**

| | 標準模式（SplitRight/Standard） | Dashboard 模式 |
|---|---|---|
| Shell UI | 完整（Header + LeftNav + BottomBar） | 無 |
| TopBar | 60px AppHeader（含登出/導航） | 32px 極簡 TopBar |
| 頁籤切換 | 多頁時顯示頁籤列 | 隱藏（固定顯示第一頁） |
| 視窗大小 | Maximized（全螢幕） | 固定 = canvas 寬 × (canvas 高 + 32px) |
| 縮放 | 可縮放（ScrollViewer + ScaleTransform） | 無（原始尺寸） |
| 使用者管理 | 可選 | 不顯示 |

### 6.2 設定方式（設計師）

1. 開啟 **MachinePageDesigner**
2. 工具列 **Layout** 下拉選單選擇 `Dashboard`
3. 設定畫布尺寸（建議與目標螢幕解析度相同，例如 1920×1080）
4. 拖曳控制項、設計版面
5. `Ctrl+S` 儲存 `.machinedesign.json`

儲存後 JSON 的 `layout.mode` 欄位會寫入 `"Dashboard"`：

```json
{
  "version": "2.0",
  "layout": { "mode": "Dashboard" },
  "pages": [
    {
      "canvasWidth": 1920,
      "canvasHeight": 1080,
      "canvasItems": [...]
    }
  ]
}
```

### 6.3 部署方式（工程師）

與標準 DesignPlayer 部署流程完全相同，**不需額外設定**：

1. 編輯 `Config/app-config.json`，將 `designFile` 指向設計師交付的 `.machinedesign.json`
2. 啟動 `Stackdose.App.DesignPlayer.exe`

DesignPlayer 讀取到 `layout.mode: "Dashboard"` 後**自動切換**為 Dashboard 模式。

### 6.4 Dashboard TopBar 說明

| 元素 | 說明 |
|---|---|
| ● 狀態燈 | `PlcStatusIndicator`：綠色 = 已連線，紅色 = 斷線（自動重連不影響） |
| 設備名稱 | 取自 `app-config.json` 的 `headerDeviceName` |
| `HH:mm:ss` 時鐘 | 每秒更新 |
| `—` 最小化 | 將視窗最小化到工作列 |
| `✕` 關閉 | 關閉應用程式 |

> TopBar 可拖曳移動視窗（點住 TopBar 空白處拖曳）。

### 6.5 視窗尺寸規則

視窗固定尺寸 = **canvas 寬度 × (canvas 高度 + 32px)**，啟動時自動置中於主螢幕。

`ResizeMode = CanMinimize`：只允許最小化，無法調整大小或最大化。

---

## 7. 常見問題

**Q: 設計時控制項不顯示數值？**
A: 正常，MachinePageDesigner 和 DesignViewer 不連 PLC，顯示 DefaultValue。

**Q: 儲存後在 DesignRuntime / DesignPlayer 看不到更新？**
A: 兩者均支援 JSON 熱更新（FileSystemWatcher）。儲存後約 400~800ms 內會自動重載。若未觸發，確認 DesignPlayer 的 `Config/app-config.json` 中 `designFile` 路徑正確，且 DesignRuntime 已手動開啟過同一個檔案。

**Q: 拖曳時控制項跳到奇怪位置？**
A: 確認 Snap 設定，Snap 啟用時會吸附格線（可在工具列關閉）。
