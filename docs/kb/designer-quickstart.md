# 設計師 Quick Start — 從零到量產部署

> 目標讀者：第一次使用 MachinePageDesigner 的設計師或工程師。
> 讀完本文你就能獨立製作一張機台監控介面，並部署給客戶。

---

## 角色分工

一個典型的設備 UI 專案分為兩個角色：

| 角色 | 做什麼 | 使用工具 |
|---|---|---|
| **工程師** | 定義 PLC 地址清單（Tags）、設定 DesignPlayer 連線參數 | MachinePageDesigner（Tags 功能）、app-config.json |
| **設計師** | 拖曳控制項、設計畫面、存檔 | MachinePageDesigner |

兩者可以同一人，也可以分開。

---

## 整體流程

```
[工程師] 定義 PLC Tags
        ↓
[設計師] MachinePageDesigner 拖曳設計
        ↓
        儲存 .machinedesign.json
        ↓
[驗證]  DesignViewer 靜態預覽（可選）
        ↓
[驗證]  DesignRuntime 連真實 PLC 驗證（可選）
        ↓
[部署]  DesignPlayer 量產 Shell App
```

---

## Step 1：工程師設定 PLC Tags

> 如果你是自己一個人做（身兼工程師和設計師），先做這步。

1. 開啟 `Stackdose.Tools.MachinePageDesigner`（啟動專案）
2. 在 Toolbar 點「📌 Tags」
3. 新增列，填入：
   - **PLC 地址**：如 `D100`、`M10`（必填）
   - **名稱**：如 `溫度感測器`、`啟動信號`
   - **單位**：如 `℃`、`rpm`（選填）
4. 按「確定」關閉視窗
5. **Ctrl+S 存檔**（Tags 存在 `.machinedesign.json` 的 `tags[]` 欄位中）

之後設計師在屬性面板填寫地址時，就能從下拉選取，不用記或查 PLC 地址表。

---

## Step 2：設計師拖曳控制項

### 2.1 認識介面

```
┌─────────────┬─────────────────────────────┬────────────────┐
│  Toolbox    │        自由畫布              │  Properties    │
│             │                              │                │
│ ├ 控制項    │  ← 拖曳控制項到這裡 →       │  選取控制項後  │
│ └ 模板庫    │                              │  在此設定屬性  │
└─────────────┴─────────────────────────────┴────────────────┘
```

### 2.2 新增控制項

從左側 **Toolbox** 拖曳任一控制項到畫布中間。

**常用控制項速查：**

| 控制項 | 用途 | 主要屬性 |
|---|---|---|
| `PlcLabel` | 顯示 PLC 數值（大字） | 位址、標籤、色彩主題、格式 |
| `PlcText` | 顯示 PLC 數值（文字行） | 位址、標籤 |
| `PlcStatusIndicator` | 顯示位元狀態（燈號） | 位址（M/D）、標籤 |
| `SecuredButton` | 需登入才能按的操作按鈕 | 命令位址、命令類型、所需權限 |
| `StaticLabel` | 靜態文字標籤 | 文字內容、字體大小、對齊 |
| `GroupBox` (Spacer) | 分組框（視覺分組） | 群組標題 |
| `AlarmViewer` | 報警清單 | 內嵌或外部 alarms.json |
| `SensorViewer` | 感測器狀態 | 內嵌或外部 sensors.json |
| `LiveLog` | 即時操作日誌 | 無需設定 |

> **模板庫**：Toolbox 切換至「模板」頁籤，有 9 個預建組合（溫度計、壓力計、啟停按鈕組等），拖入畫布後可直接修改屬性。

### 2.3 設定屬性

1. 點選畫布中的控制項（藍框出現即為選中）
2. 右側 **Properties 面板** 出現對應屬性
3. **PLC 位址欄** 為下拉輸入框：
   - 直接輸入地址（如 `D100`）
   - 或點下拉選取已定義的 Tags

### 2.4 調整位置與大小

- 拖曳控制項移動位置（Snap 自動吸附格線）
- 拖曳控制項邊角縮放大小
- Properties 面板直接輸入 X / Y / W / H 數值
- Toolbar 有「對齊」按鈕（靠左、靠上、水平置中等）

### 2.5 多頁面管理

- 頁籤列下方「+」鈕 → 新增頁面
- 雙擊頁籤名稱 → 重新命名
- 頁籤右側「×」→ 刪除頁面（至少保留一頁）
- 每頁有獨立的畫布尺寸，在 Toolbar 的 Canvas W/H 欄修改

---

## Step 3：存檔

**Ctrl+S** 儲存為 `.machinedesign.json`。

第一次會跳出「另存新檔」對話框，建議存到：

```
YourProject/
  Config/
    monitor.machinedesign.json   ← 設計稿
    alarms.json                  ← 報警定義（若有 AlarmViewer）
    sensors.json                 ← 感測器定義（若有 SensorViewer）
```

> `alarms.json` 和 `sensors.json` 會在存檔時自動輸出到和 `.machinedesign.json` 相同的目錄。

---

## Step 4：快速預覽（DesignViewer）

不需要連 PLC，直接確認畫面排版：

1. 開啟 `Stackdose.Tools.DesignViewer`
2. 把 `.machinedesign.json` 檔案拖進視窗
3. 靜態渲染畫面，支援多頁面切換

---

## Step 5：量產部署（DesignPlayer）

### 5.1 設定 app-config.json

DesignPlayer 的 `Config/app-config.json`：

```json
{
  "appTitle": "我的機台 Monitor",
  "headerDeviceName": "MACHINE-01",
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
| `appTitle` | 視窗標題列顯示名稱 |
| `loginRequired` | `true` 啟用登入管控，`false` 直接進入 |
| `plc.ip` | PLC 的 IP（FX3U 系列） |
| `plc.port` | 通訊埠（預設 3000） |
| `plc.pollIntervalMs` | 輪詢間隔（毫秒，建議 500） |
| `designFile` | 相對於執行檔的路徑 |

### 5.2 複製設計稿

將以下檔案放到 `DesignPlayer/Config/` 目錄：

```
DesignPlayer.exe
Config/
  app-config.json
  monitor.machinedesign.json
  alarms.json        ← 若有 AlarmViewer
  sensors.json       ← 若有 SensorViewer
```

### 5.3 執行

直接雙擊 `DesignPlayer.exe`，自動連線 PLC，讀取設計稿，啟動監控介面。

---

## Hot-Reload（熱更新）

DesignPlayer 執行中，設計師修改並存檔 `.machinedesign.json` 後：

- **800ms 防抖後自動重新載入**，不需要重啟 App
- 頁面切換位置會自動保留（不會跳回第一頁）
- 適合現場快速調整版型

---

## 常見問題

### Q：拖曳控制項沒有反應？
確認是從 **Toolbox 面板**拖曳（左側），不是從畫布拖曳。

### Q：PLC 位址欄沒有下拉選項？
工程師還沒定義 Tags。點 Toolbar「📌 Tags」先新增，或直接手動輸入地址。

### Q：DesignPlayer 啟動後畫面是空白？
檢查 `designFile` 路徑是否正確，以及 `.machinedesign.json` 是否存在。

### Q：控制項數值沒有更新？
確認 PLC IP/Port 正確，且 `autoConnect: true`。狀態列左側的 PLC 連線燈號應為綠色。

### Q：存檔後 alarms.json 沒出現？
只有畫布中有放 `AlarmViewer` 控制項，且定義了報警項目，存檔才會輸出 `alarms.json`。

### Q：想要登入管控？
`app-config.json` 設 `"loginRequired": true`，啟動後會先顯示登入頁面。預設帳號由 ComplianceContext 管理。

---

## 完整工作流程示範

```
1. 工程師確認 PLC 地址清單
   → 打開設計器，📌 Tags 輸入：D100(溫度)/D101(壓力)/M10(啟動)

2. 設計師拖曳
   → 拖入「溫度計」模板 → 展開 3 個 PlcLabel
   → 點選第一個 PlcLabel → Address 下拉選「D100 — 溫度感測器」
   → 調整位置、大小、配色

3. 存檔
   → Ctrl+S → monitor.machinedesign.json

4. 驗證
   → DesignViewer 靜態預覽（OK？繼續）
   → DesignRuntime 連 PLC 確認數值（工程師）

5. 部署
   → 複製 monitor.machinedesign.json → DesignPlayer/Config/
   → 設定 app-config.json（IP/Port）
   → 交付 DesignPlayer.exe + Config/ 整個資料夾
```

---

*相關文件：*
- `docs/kb/designer-system.md` — 設計器系統架構詳解
- `docs/kb/controls-reference.md` — 26 個控制項完整參考
- `docs/kb/deviceframework-guide.md` — 工程師複雜設備邏輯指南
