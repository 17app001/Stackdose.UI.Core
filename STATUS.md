# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-04
- **分支：** `master`
- **上次做了什麼：** 
    - **TabPanel 完備化：** 修正運行時 Canvas 定位、JSON 屬性大小寫不匹配；實作 `TabPanelEditorDialog` 子項目設計器，支援雙擊編輯子畫布。
    - **視覺風格同步：** GroupBox (Spacer) 改為半透明科技感風格，全專案（Designer/Runtime/Scaffold）對齊。
    - **編碼與亂碼修復：** 修正 scaffold 腳本產出檔案缺少 UTF-8 BOM 導致的中文亂碼問題，腳本本身亦強制轉為 UTF-8 BOM。
    - **Viewer 控制項優化：** Alarm/Sensor Viewer 邊框與標題列完全對齊 LogViewer (Cyber/Log 混合風格)，增加 `viewerTitle` 屬性讀取與簡約版預設值 (`ALARM`/`SENSOR`)。
    - **Scaffold 強化：** `init-shell-app.ps1` 產出的 `RuntimeControlFactory` 補全 `viewerTitle` 讀取邏輯。
- **下一步：** 實機傳圖驗證、大型檔案傳輸壓力測試。

## 進行中

| 任務 | 狀態 | 備註 |
|---|---|---|
| 大檔案傳圖壓力測試 | 待測試 | 進度條已實作，尚未用大檔案驗證 |
| PrintHeadController 傳圖流程實機驗證 | 待測試 | DPI設定/方向PLC/完成通知均已實作 |
| TabPanel / GroupBox / Encoding 實機驗證 | ✅ 完成 | 透過 new-app 重新產生專案驗證 OK |

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
