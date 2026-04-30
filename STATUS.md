# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-04-30
- **分支：** `master`
- **上次做了什麼：** PrintHead 完整整合修復（firmware config 欄位、Config 路徑、DesignRuntime 5 控件）；PrintHeadStatus 重設計為常態顯示緊湊版型；index.html 時間軸與進度補更
- **下一步：** 大檔案傳圖進度條壓力測試；ProcessStatusIndicator / PlcDeviceEditor 在 DesignRuntime 的端對端驗證

## 進行中

| 任務 | 狀態 | 備註 |
|---|---|---|
| 大檔案傳圖壓力測試 | 待測試 | 進度條已實作，尚未用大檔案驗證 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| ProcessStatusIndicator / PlcDeviceEditor DesignRuntime 渲染未實機驗證 | Low | 程式碼已加，邏輯未跑過 |

## 最近 Commits

```
950d399 docs: update index.html — add 4/29~30 timeline entry and DesignRuntime 11/11
514d71e style: clean up PrintHeadStatus data panel typography
789b477 refactor: replace PrintHeadStatus expand/collapse with always-visible compact panel
a1a9311 fix: update scaffold feiyang_head1.json template to correct field names
b3e2e85 feat: supplement DesignRuntime with missing controls and add JSON hot reload
161a52d fix: unify config file path resolution to use Config/ directory
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
