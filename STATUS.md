# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-04
- **分支：** `master`
- **上次做了什麼：**
    - **TabPanel 完備化：** 修正運行時 Canvas 定位、JSON 屬性大小寫不匹配；實作 `TabPanelEditorDialog` 子項目設計器，支援雙擊編輯子畫布。
    - **視覺風格同步：** GroupBox (Spacer) 改為半透明科技感風格（`Color.FromArgb(0xCC, 0x3A, 0x56, 0xA8)`），全專案 Designer / Runtime / Scaffold 對齊。
    - **AlarmViewer / SensorViewer 修正：** JSON case-insensitive 反序列化、group header 改為青色左邊框（`#00E5FF`），`Sensor.Bg.Header` token 調亮至 `#1B2B45`。
    - **Scaffold 強化：** sample config 含範例資料、`-AutoFullPack` 一鍵完整佈局、`viewerTitle` 屬性讀取。
    - **ButtonTheme 命名空間修正：** `Stackdose.UI.Core.Models.ButtonTheme`（非 Controls）。
    - **AI 文件：** 新增 `docs/AI_SCAFFOLDING_GUIDE.md` 標準化 scaffold 規範。
- **下一步：** 實機傳圖驗證、大型檔案傳輸壓力測試。

## 進行中

| 任務 | 狀態 | 備註 |
|---|---|---|
| 大檔案傳圖壓力測試 | 待測試 | 進度條已實作，尚未用大檔案驗證 |
| PrintHeadController 傳圖流程實機驗證 | 待測試 | DPI設定/方向PLC/完成通知均已實作 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| ProcessStatusIndicator / PlcDeviceEditor DesignRuntime 渲染未實機驗證 | Low | 程式碼已加，邏輯未跑過 |

## 最近 Commits

```
8dd306c 完成更新動作（GroupBox/Viewer 視覺同步、Scaffold 強化）
789dee4 fix: scaffold creates sample alarm/sensor configs + GroupBox header contrast
7012e80 fix: improve group header visibility in AlarmViewer and SensorViewer
6aa3b55 fix: use case-insensitive JSON deserialization in AlarmViewer
6949117 fix: correct ButtonTheme namespace (Models not Controls) in scaffold
1979273 fix: apply ButtonTheme prop in scaffold CreateSecuredButton
```

## 功能完成狀態快照（2026-05-04）

| 模組 | 完成度 |
|---|---|
| 核心框架 PLC / 日誌 / 權限 | 11 / 11 ✅ |
| MachinePageDesigner | 19 / 19 ✅（含 TabPanel + 子項設計器） |
| DesignRuntime + DesignViewer | 12 / 12 ✅ |
| 開發工具（ProjectGenerator）| 6 / 6 ✅ |
| PrintHead 整合 | ✅ 完成 |
| Dashboard 模式 | ✅ 完成（2026-04-23） |
| 底層重構 B0–B9 | ✅ 完成（2026-04-21~23） |
| TabPanel 控件 + 子項設計器 | ✅ 完成（2026-05-04） |
| Scaffold AutoFullPack | ✅ 完成（2026-05-04） |
| GroupBox / Viewer 視覺對齊 | ✅ 完成（2026-05-04） |
