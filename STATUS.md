# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-07
- **分支：** `master`
- **上次做了什麼：** Light/Dark 主題切換全面修復（完成）
  - **根本原因 1：** `LightTheme.xaml` / `Colors.xaml` / `LightColors.xaml` 的 Semantic Alias Token 使用 `{Binding Source={StaticResource X}, Path=Color}` 在 WPF Freezable 上靜默失效 → 全部改為直接 Color 值
  - **根本原因 2：** `Services.ThemeManager` 修改 nested dict（Theme.xaml 內部的 Colors.xaml），WPF DynamicResource 通知無法可靠傳播 → 改為直接 `merged.Add(LightTheme.xaml)` 到 Application.Resources 頂層
  - **Light 模式白底：** `Surface.Bg.Page` / `Surface.Bg.Panel` / `Surface.Bg.Card` 在兩個 Light 主題檔改為 `#FFFFFF`
  - **Designer placeholder 修復：** `DesignTimeControlFactory.cs` LiveLog/AlarmViewer/SensorViewer/TabPanel 全改 `SetResourceReference`；`SystemClock.xaml` 移除硬編碼 Style
  - **PrintHeadController 按鈕：** 讀取圖片/載入任務/取消任務 改為 Material Design 高飽和度色（#42A5F5 / #FFA726 / #EF5350）
  - **主題切換機制說明：** DesignRuntime 主題存於 JSON `layout.theme`，載入時自動套用；實機 App 可呼叫 `ThemeManager.SwitchTheme()` 切換
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
[本次] fix: Light/Dark theme complete — Freezable Binding + ThemeManager top-level override + white bg + PrintHead buttons
dacae6b fix: ThemeManager NotifyOwners + FreeCanvas hardcoded bg
3a5593b fix: Dashboard letterbox background + devlog/status update
06c1726 merge: Dashboard proportional maximize button
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | ✅ 完成（含容器感知、全方位 Padding） |
| Light/Dark 主題切換 | ✅ 完成（Designer + DesignRuntime + 實機 App） |
