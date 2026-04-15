# Stackdose.Tools.DesignViewer

**設計稿靜態預覽工具**。拖曳 `.machinedesign.json` 即時渲染畫布，不需連線 PLC，用於快速確認版面與樣式。

## 這是什麼

設計器三件組之一（Designer → **Viewer** → Runtime）。最輕量的預覽工具，只渲染外觀，不連線 PLC。

## 依賴

- `Stackdose.Tools.MachinePageDesigner`（共用 Models / Services）
- `Stackdose.UI.Core`

## 功能

| 功能 | 說明 |
|---|---|
| 拖曳開啟 | 將 `.machinedesign.json` 拖曳進視窗即時渲染 |
| 開啟按鈕 | 透過檔案對話框選擇 JSON |
| 縮放 | Slider 控制畫布縮放比例 |
| 錯誤占位符 | 元件建立失敗時以橘框標示，不中斷其他元件 |
| 狀態列 | 顯示畫布尺寸、元件數量 |

## 與 DesignRuntime 的差異

| | DesignViewer | DesignRuntime |
|---|---|---|
| PLC 連線 | 無 | 支援（含模擬器模式） |
| 即時數值 | 無（靜態樣式） | 有 |
| 用途 | 版面快速確認 | 上線前執行驗證 |
| 重量 | 極輕（2個檔案） | 較重 |

## 使用方式

1. 啟動 `DesignViewer`
2. 將 `.machinedesign.json` 拖曳進視窗，或點「開啟」選擇檔案
3. 確認版面、位置、控制項類型是否正確
