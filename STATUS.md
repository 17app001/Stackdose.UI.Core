# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-04
- **分支：** `master`
- **上次做了什麼：** 補上 TabPanel 全段實作並提交：TabPanel 控件、RuntimeControlFactory、DesignTimeControlFactory、ToolboxItemDescriptor、DesignerItemViewModel.TabTitles、PropertyPanel 模板、TabPanelEditorDialog（雙擊子項設計器）
- **下一步：** 實機驗證 TabPanel 雙擊 → Dialog → 儲存 → JSON round-trip

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
f4a6605 merge: feature/printhead-robustness → master
cbc4fce chore: update CLAUDE.md - remove resolved FeiyangWrapper.dll issue
a96efdf fix: select FeiyangWrapper dll by $(Configuration) instead of Debug-first
7420c71 完成waves檔案路徑設定，但實機測試會找不到feiyangwrapper.dll
4ddff86 feat: improve waveform placement, fix sensor/alarm path resolution
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
