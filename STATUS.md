# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-07
- **分支：** `master`
- **上次做了什麼：** Light 主題三項修復（完成）
  - **PlcLabel 底框不切換：** `UpdateFrameBackground()` 改為 `FrameBorder.SetResourceReference(Border.BackgroundProperty, "Plc.Bg.Main")`，DynamicResource 語意自動跟隨主題
  - **Light 模式全白問題：** `LightTheme.xaml` + `LightColors.xaml` 的 Surface tokens 改為分層灰色（Page `#EBEBEB` / Panel `#F5F5F5` / Card `#FAFAFA` / Control `#FFFFFF`），與深色模式層次對應
  - **RuntimeControlFactory Spacer 透明：** `CreateGroupBox()` body border 改為 `SetResourceReference(Border.BackgroundProperty, "Surface.Bg.Card")`，固態背景不再透空
- **下一步：**
    1. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框，與 Events 系統接軌
    2. **ModelE 實機驗證** — 噴頭啟動接線與 M-bit 事件連動測試
    3. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重新載入畫布

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
