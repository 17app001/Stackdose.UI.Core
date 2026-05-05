# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-05 (下班前更新)
- **分支：** `master`
- **上次做了什麼：**
    - **Designer 體驗大幅強化：**
        - **Spacer (GroupBox) 容器化：** 實現「深層選取」，複製/剪下/刪除容器時自動連動內部元件。
        - **TabPanel 子設計器升級：** 支援跨視窗剪貼簿、快捷鍵 (Ctrl+X/C/V/Z/Y)、屬性面板與左上角智慧貼上。
        - **間距與校正系統：** 實作全域 Gap 與 Canvas Padding 設定；新增「✨ Precision Gap」一鍵精密校準與「水平/垂直堆疊」工具。
        - **視覺優化：** Spacer 框線/背景隨標題色連動；新增「隱藏標題」功能。
    - **Runtime 視覺校正：** 實作「視窗高度補償」，解決標題列擋住畫布底部的 WYSIWYG 問題。
- **下一步（明日上工順序）：**
    1. **Canvas Padding 修正** — 解決 AlarmViewer 與 SensorViewer 在精密校正時不理會 Padding 的問題。
    2. **PlcConfirmationHandler** — 實作倒數確認對話框，與 Events 系統接軌。
    3. **ModelE 實機驗證** — 噴頭啟動接線與 M-bit 事件連動測試。

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | Canvas Padding 修正 | `MainViewModel.cs` | Alarm/Sensor 鄰居偵測算法微調 |
| 2 | PlcConfirmationHandler | `ModelE/Handlers/PlcConfirmationHandler.cs` | 對應 WinForms PlcMessageForm |
| 3 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| JSON 熱更新（修改 JSON 後自動重新載入畫布） | 中 | DesignRuntime 尚未實作 |
| **Canvas Padding 對特定元件無效** | 高 | AlarmViewer/SensorViewer 需明天修正 |

## 最近 Commits

```
9456591 feat: implement Global Spacing (Gap) system and Gap Snapping
9539006 feat: add dedicated Canvas Padding setting and upgrade Precision Gap
3f4e782 feat: Spacer 'Hide Title' feature and final designer refinements
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | 🟡 進行中（Padding 尚有小 Bug） |
