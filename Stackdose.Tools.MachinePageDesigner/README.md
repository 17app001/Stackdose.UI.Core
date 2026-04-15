# Stackdose.Tools.MachinePageDesigner

**視覺化機台頁面設計器**。以自由畫布（FreeCanvas）模式拖曳 PLC 控制項，設定位址屬性後儲存為 `.machinedesign.json`，供 DesignViewer 預覽或 DesignRuntime 執行。

## 這是什麼

設計器三件組的核心工具（**Designer** → Viewer → Runtime）。讓不熟悉 XAML 的工程師也能透過拖曳建立機台監控頁面。

## 依賴

- `Stackdose.UI.Core`

## 功能總覽

| 類別 | 功能 |
|---|---|
| 自由畫布 | 控制項可拖曳至任意位置 |
| 右鍵 ContextMenu | 複製/貼上/刪除/鎖定/Z-Order 快速操作 |
| 工具箱 | 左側 Toolbox 含 9 種控制項類型 |
| 屬性面板 | 右側 PropertyPanel 每種控制項均有專屬屬性表單 |
| 多選操作 | Shift+Click 或框選，同步移動/縮放 |
| Smart Snap | 拖曳時磁吸對齊其他控制項邊緣/中心 |
| 對齊分配 | 左/右/上/下對齊，水平/垂直均等分配 |
| GroupBox | 將控制項群組化，整體移動 |
| 複製貼上 | Ctrl+C / Ctrl+V |
| Snap 格線 | 拖曳時自動吸附格線 |
| Z-Order | 調整控制項前置/後置層級 |
| 鎖定 | 鎖定完成控制項防止誤移 |
| Undo/Redo | 支援多步還原重做（Ctrl+Z / Ctrl+Y） |
| 畫布尺寸 | 可設定畫布寬高（px） |

## 支援控制項（9 種）

| 控制項 | 分類 | 說明 |
|---|---|---|
| `PlcLabel` | PLC | 數值顯示，色彩主題 / 框形 / 除數換算 |
| `PlcText` | PLC | 文字顯示 |
| `PlcStatusIndicator` | PLC | 位元狀態燈 |
| `SecuredButton` | Button | 需授權操作按鈕 |
| `StaticLabel` | Layout | 靜態文字（標題/說明），可設字體大小/粗細/對齊/顏色 |
| `Spacer` | Layout | GroupBox 群組框 |
| `LiveLog` | Viewer | 即時日誌面板（無需設定） |
| `AlarmViewer` | Viewer | 警報列表（configFile 指向 alarms.json） |
| `SensorViewer` | Viewer | 感測器列表（configFile 指向 sensors.json） |

## 核心類別

| 類別 | 職責 |
|---|---|
| `DesignDocument` | 畫布文件資料模型（CanvasWidth/Height + CanvasItems） |
| `DesignerItemDefinition` | 單一控制項定義（Type, X, Y, Width, Height, Properties） |
| `DesignFileService` | `.machinedesign.json` 的儲存與載入 |
| `UndoRedoService` | 操作歷史管理 |
| `DesignerItemViewModel` | 單一控制項的 ViewModel（包含選取、鎖定狀態） |
| `FreeCanvasItem` | 畫布上可拖曳的控制項容器 |

## 輸出格式

儲存的 `.machinedesign.json` 範例：

```json
{
  "canvasWidth": 1200,
  "canvasHeight": 800,
  "canvasItems": [
    {
      "type": "PlcLabel",
      "x": 100, "y": 80,
      "width": 160, "height": 60,
      "properties": { "Address": "D100", "Label": "溫度" }
    }
  ]
}
```

## 使用流程

1. 啟動 `MachinePageDesigner`
2. 從左側 Toolbox 拖曳控制項至畫布
3. 在右側 PropertyPanel 設定 PLC 位址與顯示屬性
4. Ctrl+S 儲存為 `.machinedesign.json`
5. 拖曳至 `DesignViewer` 預覽，或用 `DesignRuntime` 連線驗證
