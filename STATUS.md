# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-07
- **分支：** `master`
- **上次做了什麼：** JSON 驅動主題切換與 Light 模式優化（進度中）
  - **自動化：** 實現由 `machinedesign.json` 定義主題，Runtime 自動呼叫 `ThemeManager` 切換，並更新 `new-app.ps1` 模板支援動態資源。
  - **資源重構：** 統一 `LightColors.xaml` 資源 Key，解決多項組件在淺色模式下的慘白問題。
- **⚠️ 待解決 UI 問題：**
  - **AlarmViewer 背景異常：** 在 Light 模式下仍顯示深色背景，需檢查資源引用鏈。
  - **按鈕文字顏色：** 彩色按鈕（Primary/Success等）文字在 Light 模式下未正確維持白色，對比度不足。
- **下一步：**
    1. **修復 Alarm/Sensor Viewer 視覺一致性** — 確保背景與文字 100% 隨主題切換
    2. **修正按鈕 Foreground 邏輯** — 強制彩色背景按鈕使用白色文字
    3. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `UI.Core/Shell/Handlers` | 帶倒數與 Event 接軌的確認框 |
| 2 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、direction + D512 flag 確認 |
| 3 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| **D512 PLC flag 缺失** | 中 | ModelE 傳圖前寫 D512 作為層旗標，待實機確認是否需要補 |
| JSON 熱更新 | 中 | DesignRuntime 尚未實作 |

## 最近 Commits

```
[本次] fix: PlcLabel theme + Light surface gray + RuntimeFactory Spacer solid bg
[前次] fix: Light/Dark theme complete — Freezable Binding + ThemeManager top-level override + white bg + PrintHead buttons
dacae6b fix: ThemeManager NotifyOwners + FreeCanvas hardcoded bg
3a5593b fix: Dashboard letterbox background + devlog/status update
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | ✅ 完成（含容器感知、全方位 Padding） |
| Light/Dark 主題切換 | ✅ 完成（Designer + DesignRuntime + 實機 App） |
