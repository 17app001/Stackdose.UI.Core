# Stackdose.Tools.ProjectGeneratorUI

**專案產生器 GUI 版本**。以視覺化表單填入機台規格，點「產生」即可輸出完整 DeviceFramework App 專案，底層呼叫 `ProjectGenerator` 函式庫。

## 這是什麼

`ProjectGenerator` 的圖形化前端，提供比 CLI 更友善的操作體驗，並含版面預覽功能。

## 依賴

- `Stackdose.Tools.ProjectGenerator`（核心產生邏輯）
- `Stackdose.UI.Core`（主題與控制項）

## 功能

| 功能 | 說明 |
|---|---|
| 專案設定 | 填入 ProjectName、HeaderDeviceName、Version、PageMode、LayoutMode |
| 機台管理 | 新增/刪除機台，設定 PLC IP/Port、Commands、Labels |
| 版面預覽 | `LayoutPreviewControl` 即時預覽版面配置 |
| 一鍵產生 | 填完後點「產生專案」，輸出至指定目錄 |
| 預覽區 | 使用真實 `PlcDataGridPanel` 渲染 Labels 預覽（不連線 PLC） |

## PageMode 選項

| 模式 | 說明 |
|---|---|
| `DynamicDevicePage` | 框架預設頁面，JSON 驅動，零 C# |
| `SinglePage` | 單頁簡易版 |
| `CustomPage` | 產生 CustomPage.xaml 骨架，供自行擴充 |

## LayoutMode 選項

| 模式 | 說明 |
|---|---|
| `SplitRight` | 右側指令欄 + 左側數據（預設） |
| `Standard` | 標準上下布局 |
| `SplitBottom` | 下方指令欄 |

## 核心類別

| 類別 | 職責 |
|---|---|
| `MainViewModel` | 表單狀態管理（Project 設定 + Machines 集合） |
| `MachineViewModel` | 單一機台欄位（MachineId、PlcIp、Commands、Labels） |
| `LayoutPreviewControl` | 版面配置視覺預覽控制項 |
