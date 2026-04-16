# Stackdose.App.DesignRuntime

**設計器執行時期驗證 App**。載入 `.machinedesign.json` 並連線真實（或模擬）PLC，讓控制項顯示即時數值，用於設計完成後的執行期驗證。

## 這是什麼

設計器三件組之一（Designer → Viewer → **Runtime**）。與 DesignViewer 的差別：**可連線 PLC 顯示真實數值**，是上線前的最終驗證工具。

## 依賴

- `Stackdose.Tools.MachinePageDesigner`（共用 Models / Services）
- `Stackdose.UI.Core`
- `Stackdose.Hardware.Plc`（Platform 外部依賴）

## 功能

| 功能 | 說明 |
|---|---|
| PLC 連線 | 輸入 IP / Port / ScanInterval，點「連線」即可 |
| 模擬器模式 | 勾選「模擬器模式」不需真實 PLC 即可連線 |
| 載入 JSON | 選擇或拖曳 `.machinedesign.json` 即時渲染畫布 |
| 縮放 | Slider 控制畫布縮放比例 |
| 亂數注入 | 「亂數 D100~D102」按鈕每 100~300ms 隨機寫入測試值（Toggle） |
| 錯誤占位符 | 元件建立失敗時以橘框標示，不中斷其他元件載入 |
| JSON 熱更新 | FileSystemWatcher 偵測變更後自動重載畫布（防抖 800ms） |
| **PLC 斷線重連** | `ConnectionLost` 事件觸發 → UI 顯示警告 → 自動重連 → `RefreshMonitors()` |

## 使用流程

1. 用 `MachinePageDesigner` 完成版面設計並儲存 `.machinedesign.json`
2. 啟動 `DesignRuntime`
3. 輸入 PLC IP 並連線（或啟用模擬器模式）
4. 拖曳 JSON 檔案進視窗，確認數值正常顯示

## 開發狀態

Active。JSON Hot-Reload 與 PLC 斷線重連均已完成。
下一步：讀取 `pages` 陣列支援多頁面切換（目前僅讀第一頁）。
