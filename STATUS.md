# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-08
- **分支：** `master`
- **上次做了什麼：** Light 模式視覺一致性第三輪修正（截圖回饋後）
  - `PrintHeadController.xaml` — 移除 local `Theme.xaml` merge（WPF scope 遮蔽 Light override 的根因），ComboBoxItem `Foreground="White"` → `Text.Primary`
  - `DesignTimeControlFactory.cs` — TabPanel placeholder 加外層 Border（`Log.Border` + `CornerRadius=6`）；Spacer 預設 headerColor 改 `"Normal"`（灰）
  - `RuntimeControlFactory.cs` — Spacer 預設 headerColor 同步改 `"Normal"`
  - `LightColors.xaml` — `Plc.Bg.Main` `#FFFFFF` → `#F5F7F9`（PlcLabel 預設背景改 card level 灰）
- **下一步：**
    1. 在 PageDesigner 重新截圖確認 Light 模式一致性
    2. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框
    3. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重載畫布

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
