# MachinePageDesigner 使用說明

> 版本：v1.0 | 適用於 Stackdose.Tools.MachinePageDesigner

---

## 1. 概述

MachinePageDesigner 是一個獨立的 WPF 視覺化頁面設計工具，用於以**拖拉方式**將 PLC 控制項組合成 MachinePage，並輸出為 `.machinedesign.json` 設計檔。

---

## 2. 啟動

直接執行 `Stackdose.Tools.MachinePageDesigner.exe`，啟動後會顯示主視窗，包含三大區域：

| 區域 | 位置 | 說明 |
|---|---|---|
| **Toolbox（工具箱）** | 左側 | 可拖拉的控制項清單 |
| **Design Canvas（設計畫布）** | 中央 | Zone 區塊排列，放置控制項 |
| **Properties（屬性面板）** | 右側 | 編輯選取控制項的屬性 |

---

## 3. 工具列按鈕

| 按鈕 | 快捷鍵 | 說明 |
|---|---|---|
| New | `Ctrl+N` | 建立新的空白設計檔 |
| Open | `Ctrl+O` | 開啟既有 `.machinedesign.json` |
| Save | `Ctrl+S` | 儲存目前設計檔 |
| Save As | `Ctrl+Shift+S` | 另存新檔 |
| Undo | `Ctrl+Z` | 復原上一步操作 |
| Redo | `Ctrl+Y` | 重做已復原的操作 |
| Delete | `Delete` | 刪除選取的控制項 |

工具列右側可設定：
- **Layout** — 頁面版型（SplitRight / Standard / SplitBottom）
- **Title** — 設計檔標題
- **Machine ID** — 機台識別碼

---

## 4. 基本操作流程

### 4.1 拖拉控制項進入 Zone

1. 在左側 **Toolbox** 找到要使用的控制項（如 PlcLabel）
2. 按住滑鼠左鍵拖動到中央畫布的 Zone 區塊上
3. 放開滑鼠，控制項會自動加入 Zone 並以 UniformGrid 排列

### 4.2 選取控制項

- **單選**：直接點擊控制項卡片
- **多選**：按住 `Shift` 鍵同時點擊多個控制項

選取後，右側屬性面板會顯示該控制項的可編輯屬性。

### 4.3 編輯屬性

選取控制項後，在右側 **Properties** 面板中修改：

| 控制項類型 | 可編輯屬性 |
|---|---|
| **PlcLabel** | 標籤名稱、PLC 位址、預設值、字體大小、外框形狀（矩形/圓形）、色彩主題、除數、數值格式 |
| **PlcText** | 標籤名稱、PLC 位址 |
| **PlcStatusIndicator** | 顯示位址 |
| **SecuredButton** | 按鈕名稱、命令位址、權限等級、按鈕主題 |
| **Spacer** | 無屬性（僅佔位用） |

修改後，畫布上的控制項預覽會即時更新。

### 4.4 Zone 內拖拉排序

每個控制項卡片左下角有一個 **☰ 拖拉把手**，按住即可拖動控制項以調整順序。

### 4.5 Zone 設定

每個 Zone 標題列可設定：
- **標題**：直接編輯文字
- **欄數**：下拉選擇 1~6 欄，控制項會自動等寬排列

### 4.6 刪除控制項

- 點擊控制項卡片右下角的 **✕** 按鈕
- 或選取後按鍵盤 `Delete` 鍵
- 支援多選後一次刪除多個

所有操作皆支援 Undo/Redo。

---

## 5. 版型模式

透過工具列的 **Layout** 下拉選單切換：

| 模式 | 說明 |
|---|---|
| **SplitRight** | 第一個 Zone 在左側（佔 40%），其餘 Zone 在右側堆疊 |
| **SplitBottom** | 第一個 Zone 在上方（佔 60%），其餘 Zone 在下方 |
| **Standard** | 所有 Zone 垂直堆疊排列 |

---

## 6. 儲存與載入

### 儲存
- `Ctrl+S` 儲存至目前路徑（首次會跳出另存新檔對話框）
- `Ctrl+Shift+S` 另存新檔
- 檔案格式為 `.machinedesign.json`

### 載入
- `Ctrl+O` 開啟既有設計檔
- 載入後畫布會完整還原所有 Zone 和控制項

### 檔案格式

設計檔為 JSON 格式，包含：
```json
{
  "version": "1.0",
  "meta": { "title": "...", "machineId": "M1", ... },
  "layout": { "mode": "SplitRight", ... },
  "zones": {
    "liveData": { "title": "Live Data", "columns": 2, "items": [...] },
    "deviceStatus": { "title": "Device Status", "columns": 2, "items": [...] }
  }
}
```

---

## 7. 可用控制項一覽

| 類型 | 圖示 | 說明 |
|---|---|---|
| **PlcLabel** | ◈ | PLC 數值顯示標籤，支援色彩主題、外框形狀、除數換算 |
| **PlcText** | ✎ | PLC 文字輸入/顯示 |
| **PlcStatusIndicator** | ● | 狀態指示燈 |
| **SecuredButton** | ▣ | 需權限驗證的操作按鈕 |
| **Spacer** | □ | 空白佔位元素，用於排版對齊 |

---

## 8. 快捷鍵總覽

| 快捷鍵 | 功能 |
|---|---|
| `Ctrl+N` | 新建 |
| `Ctrl+O` | 開啟 |
| `Ctrl+S` | 儲存 |
| `Ctrl+Shift+S` | 另存新檔 |
| `Ctrl+Z` | 復原 |
| `Ctrl+Y` | 重做 |
| `Delete` | 刪除選取項目 |
| `Shift+Click` | 多選 |

---

## 9. 注意事項

- 設計時控制項**不連線 PLC**，PlcLabel 會顯示預設值（DefaultValue）
- Zone 使用 `UniformGrid` 自動等寬排列，無需手動調座標
- 未儲存的變更會在標題列顯示 `*` 標記
- 關閉或新建時若有未儲存變更，會提示確認
