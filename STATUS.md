# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-05
- **分支：** `master`
- **上次做了什麼：**
    - **Designer 體驗大幅強化：**
        - **Spacer (GroupBox) 容器化：** 實現「深層選取」，複製/剪下/刪除容器時自動連動內部元件。
        - **TabPanel 子設計器升級：** 支援跨視窗剪貼簿、快捷鍵 (Ctrl+X/C/V/Z/Y)、屬性面板與左上角智慧貼上。
        - **視覺與功能擴充：** Spacer 框線/背景隨標題色連動；新增「隱藏標題」功能（含設計時 Ghost Handle）。
    - **Runtime 視覺校正：** 實作「視窗高度補償」，確保設計高度不被標題列遮擋；修正 Spacer 標題與顏色讀取 Bug。
    - **ModelE 移植：** 靜態層配置完成，補齊 ViewModel 屬性通知機制。
- **下一步（明日上工順序）：**
    1. `PlcConfirmationHandler` — 實作倒數確認對話框，與 Events 系統接軌。
    2. 噴頭啟動接線 — 在 MainWindow OnLoaded 進行實機初始化驗證。
    3. 實機壓力測試 — 驗證大檔案傳圖與多軸同步狀態。

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `ModelE/Handlers/PlcConfirmationHandler.cs` | 對應 WinForms PlcMessageForm，倒數→寫回 ConfirmAddress |
| 2 | 噴頭啟動 wiring | `ModelE/MainWindow.xaml.cs` | ConnectionEstablished 事件後 init PrintHead |
| 3 | machinedesign.json M-bit events | `ModelE/Config/M1.machinedesign.json` | M9=列印中→停用btnPrint；M1/M4=初始化中→停用相關按鈕 |
| 4 | 實機驗證 | — | feiyang_head2 BoardIP、app-config PLC IP、wave 檔 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| JSON 熱更新（修改 JSON 後自動重新載入畫布） | 中 | DesignRuntime 尚未實作 |

## 最近 Commits

```
231a956 feat: GroupBox color synchronization and bug fixes
7227130 feat: enhance Spacer containment for Copy/Cut/Delete and add Cut support
61c2b51 feat: improve paste behavior for TabPanel (paste at top-left)
2a64861 feat: enhance TabPanel editor with shared clipboard and command wiring
```

## 功能完成狀態快照（2026-05-05）

| 模組 | 完成度 |
|---|---|
| 核心框架 PLC / 日誌 / 權限 | 11 / 11 ✅ |
| MachinePageDesigner | 21 / 21 ✅（含跨視窗剪貼、深層選取、視覺連動） |
| DesignRuntime + DesignViewer | 13 / 13 ✅（含視窗尺寸補償） |
| 開發工具（ProjectGenerator）| 6 / 6 ✅ |
| PrintHead 整合 | ✅ 完成 |
| Dashboard 模式 | ✅ 完成 |
| 底層重構 B0–B10 | ✅ 完成（2026-05-05） |
| TabPanel 強化版 | ✅ 完成（2026-05-05） |
| ModelE 移植 — 靜態層（JSON/Config） | ✅ 完成 |
| ModelE 移植 — 邏輯層（Handlers/Events） | 🔄 進行中 |
