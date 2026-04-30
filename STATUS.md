# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-04-30
- **分支：** `feature/theme-switching`（待合入 master）
- **上次做了什麼：** Dark/Light 主題切換實作完成；修復 ThemeManager.SwapColorDictionaries 無法匹配巢狀 ResourceDictionary 相對 URI 的 Bug（`"Colors.xaml"` vs `"pack://..."`）
- **下一步：** 實機驗證 LiveLog / PrintHeadStatus / PrintHeadController 切換效果，確認後合入 master

## 進行中

| 任務 | 狀態 | 備註 |
|---|---|---|
| Dark/Light 主題切換 | 待驗證 | 代碼完成，需實機確認 LiveLog/PrintHeadStatus/PrintHeadController 切換效果 |
| 大檔案傳圖壓力測試 | 待測試 | 進度條已實作，尚未用大檔案驗證 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| ProcessStatusIndicator / PlcDeviceEditor DesignRuntime 渲染未實機驗證 | Low | 程式碼已加，邏輯未跑過 |

## 最近 Commits

```
d50af86 fix: ThemeManager SwapColorDictionaries now matches relative URI filenames
547fd82 feat: implement runtime Dark/Light theme switching
eae25a1 docs: add STATUS.md — current project state snapshot 2026-04-30
950d399 docs: update index.html — add 4/29~30 timeline entry and DesignRuntime 11/11
514d71e style: clean up PrintHeadStatus data panel typography
```

## 功能完成狀態快照（2026-04-30）

| 模組 | 完成度 |
|---|---|
| 核心框架 PLC / 日誌 / 權限 | 11 / 11 ✅ |
| MachinePageDesigner | 18 / 18 ✅ |
| DesignRuntime + DesignViewer | 11 / 11 ✅ |
| 開發工具（ProjectGenerator）| 6 / 6 ✅ |
| PrintHead 整合 | ✅ 完成 |
| Dashboard 模式 | ✅ 完成（2026-04-23） |
| 底層重構 B0–B9 | ✅ 完成（2026-04-21~23） |
