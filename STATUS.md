# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-06
- **分支：** `master`（UI.Core + Platform 皆已推送至 origin）
- **上次做了什麼：**
    - **主題切換修正（進行中）：** 嘗試修正 MachinePageDesigner Light/Dark 切換失效問題。
      - `ThemeManager.cs`：修正 `MergedDictionaries[i] = ...` 改為 `RemoveAt + Insert`（WPF NotifyOwners bug）。
      - `FreeCanvas.xaml`：畫布背景從硬編碼 `#FF1A1A2A` 改為 `DynamicResource Surface.Bg.Card` + 透明格線 overlay。
    - 問題尚未完全解決，明日繼續（見下方未解問題）。
- **下一步（明日上工順序）：**
    1. **🔴 Light/Dark 主題切換修復（接續）** — 切換仍失效，控件 Light 版本樣式不完整，背景顏色也未跟隨主題變化。需排查：(a) `LightTheme.xaml` 覆蓋是否完整、(b) 各 UserControl 是否有用 `StaticResource` 寫死顏色、(c) ThemeManager `TryReplaceResource` 遞迴邏輯驗證。
    2. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框，與 Events 系統接軌。
    3. **ModelE 實機驗證** — 噴頭啟動接線與 M-bit 事件連動測試（direction + D512 flag 待實機確認）。
    4. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重新載入畫布。

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `UI.Core/Shell/Handlers` | 帶倒數與 Event 接軌的確認框 |
| 2 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、direction + D512 flag 確認 |
| 3 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| **🔴 Light/Dark 主題切換失效** | 高 | 切換後 UI 無變化或變化不完整；控件缺 Light 版樣式；畫布背景未跟隨。已修 ThemeManager NotifyOwners bug + FreeCanvas 背景，效果待驗證。明日繼續排查 UserControl StaticResource 問題。 |
| **D512 PLC flag 缺失** | 中 | ModelE 傳圖前寫 D512 作為層旗標，待實機確認是否需要補 |
| JSON 熱更新 | 中 | DesignRuntime 尚未實作 |

## 最近 Commits

```
[本次] fix: ThemeManager NotifyOwners + FreeCanvas hardcoded bg (主題切換部分修正)
4fb3519 refactor: relocate SystemClock to UI.Core and enhance Precision Gap layout tool
06c1726 merge: Dashboard proportional maximize button
892b600 feat: direction gap fix + devlog/status update
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | ✅ 完成（含容器感知、全方位 Padding） |
