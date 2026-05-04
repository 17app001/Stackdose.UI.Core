# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-04
- **分支：** `master`
- **上次做了什麼：** 修正 TabPanel 運行時問題（Canvas 定位、JSON 大小寫、tab 文字顯示）、GroupBox border 樣式、AlarmViewer/SensorViewer config 預設路徑、SecuredButton Theme prop 套用（DesignRuntime + scaffold）
- **下一步：** 編譯 MyPrintApp2 驗證全部修正是否生效（按鈕顏色、Alarm/Sensor 顯示、GroupBox 樣式）

## 進行中

| 任務 | 狀態 | 備註 |
|---|---|---|
| 大檔案傳圖壓力測試 | 待測試 | 進度條已實作，尚未用大檔案驗證 |
| PrintHeadController 傳圖流程實機驗證 | 待測試 | DPI設定/方向PLC/完成通知均已實作 |
| TabPanel 實機驗證 | 待測試 | 已編譯並提交，未實機測試 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| ProcessStatusIndicator / PlcDeviceEditor DesignRuntime 渲染未實機驗證 | Low | 程式碼已加，邏輯未跑過 |

## 最近 Commits

```
1979273 fix: apply ButtonTheme prop in scaffold CreateSecuredButton
05bf6ec docs: update STATUS.md after feature/theme-switching merge
fc91e65 merge: feature/theme-switching → master
a4e1a3e feat: image/print flow + JSON wiring for PrintHeadController
e511cb5 feat: add Dark/Light theme toggle to MachinePageDesigner toolbar
```

## 功能完成狀態快照（2026-04-30）

| 模組 | 完成度 |
|---|---|
| 核心框架 PLC / 日誌 / 權限 | 11 / 11 ✅ |
| MachinePageDesigner | 19 / 19 ✅（含 TabPanel） |
| DesignRuntime + DesignViewer | 12 / 12 ✅（含 TabPanel） |
| 開發工具（ProjectGenerator）| 6 / 6 ✅ |
| PrintHead 整合 | ✅ 完成 |
| Dashboard 模式 | ✅ 完成（2026-04-23） |
| 底層重構 B0–B9 | ✅ 完成（2026-04-21~23） |
| TabPanel 控件 + 子項設計器 | ✅ 完成（2026-05-04，待實機） |
