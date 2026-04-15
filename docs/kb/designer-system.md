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
| Z-Order 控制 | ✅ | 前置/後置層級調整 |
| 框選（多選） | ✅ | 拖曳框選多個控制項 |
| 鎖定 | ✅ | 鎖定控制項防止誤移 |
| 複製貼上 | ✅ | Ctrl+C / Ctrl+V |
| 多選同步 | ✅ | 多選同步移動 |
| GroupBox | ✅ | 將多個控制項組合成群組 |
| 對齊分配 | ✅ | 左對齊、水平均等分配等 |
| 畫布尺寸設定 | ✅ | 可自訂畫布寬高 |
| Undo/Redo | ✅ | Ctrl+Z / Ctrl+Y |
| 儲存/載入 | ✅ | .machinedesign.json |

### 2.3 輸出格式（.machinedesign.json）

```json
{
  "version": "1.0",
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "meta": {
    "title": "頁面標題",
    "machineId": "M1",
    "createdAt": "2026-04-15T00:00:00Z",
    "modifiedAt": "2026-04-15T00:00:00Z"
  },
  "canvasItems": [
    {
      "type": "PlcLabel",
      "x": 100, "y": 200,
      "width": 120, "height": 80,
      "properties": {
        "label": "溫度",
        "address": "D100",
        "valueColorTheme": "NeonBlue",
        "frameShape": "Rectangle"
      }
    }
  ]
}
```

> **注意：** 頂層鍵是 `canvasItems`（非 `items`），畫布尺寸也在頂層（非在 `meta` 下）。這是 `DesignDocument` Model 的實際欄位名稱。

### 2.4 支援控制項

| 控制項 | 說明 |
|---|---|
| `PlcLabel` | PLC數值顯示，支援色彩主題、外框形狀、除數換算 |
| `PlcText` | PLC文字顯示 |
| `PlcStatusIndicator` | 狀態指示燈 |
| `SecuredButton` | 需權限驗證的操作按鈕 |
| `Spacer` | 空白佔位元素 |

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

### 2.6 專案依賴
```
Stackdose.Tools.MachinePageDesigner
├── → Stackdose.UI.Core（PlcLabel 等真實控制項，WYSIWYG）
└── → Stackdose.App.DeviceFramework（DeviceLabelViewModel、PlcDataGridPanel）
```

---

## 3. DesignViewer

**專案：** `Stackdose.Tools.DesignViewer`
**狀態：** 開發中

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
**狀態：** 開發中

### 4.1 用途
開發者驗證工具：手動輸入 PLC IP、開啟 JSON，確認控制項顯示正確的實際 PLC 數值。適合設計完成後上線前的最終驗證，不適合量產部署。

### 4.2 主要功能

| 功能 | 說明 |
|---|---|
| 手動輸入 PLC IP / Port / Scan | 工具列直接輸入，無需設定檔 |
| 模擬器模式 | 勾選後無需真實 PLC 即可連線 |
| 亂數測試注入 | 自動寫入 D100~D102 隨機值，驗證數值更新 |
| JSON 熱更新 | 偵測到 .machinedesign.json 變更後自動重載畫布 |
| 縮放 | 0.25x ~ 2.0x 滑桿縮放 |

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
**狀態：** 開發中

### 5.1 用途
可交付量產的 Shell App。設備廠商直接部署到現場，不需修改程式碼，只需編輯 `Config/` 下的 JSON 設定。

### 5.2 與 DesignRuntime 的差異

| 特性 | DesignRuntime | DesignPlayer |
|---|---|---|
| 目標 | 開發者驗證 | 量產部署 |
| Shell UI | 無（裸視窗） | 完整（左側導航 + 頁首 + 頁尾） |
| PLC 設定方式 | 工具列手動輸入 | `app-config.json` 外部設定 |
| 登入管控 | 無 | 可選（`loginRequired`） |
| 使用者管理 | 無 | 內建（Operator 以上） |
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

## 6. 常見問題

**Q: 設計時控制項不顯示數值？**
A: 正常，MachinePageDesigner 和 DesignViewer 不連 PLC，顯示 DefaultValue。

**Q: 儲存後在 DesignRuntime / DesignPlayer 看不到更新？**
A: 兩者均支援 JSON 熱更新（FileSystemWatcher）。儲存後約 400~800ms 內會自動重載。若未觸發，確認 DesignPlayer 的 `Config/app-config.json` 中 `designFile` 路徑正確，且 DesignRuntime 已手動開啟過同一個檔案。

**Q: 拖曳時控制項跳到奇怪位置？**
A: 確認 Snap 設定，Snap 啟用時會吸附格線（可在工具列關閉）。
