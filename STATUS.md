# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-08
- **分支：** `master`
- **上次做了什麼：** Light 模式 runtime 完整修正（2026-05-08）
  - `AlarmViewer.xaml` / `LogManagementPanel.xaml` / `UserManagementPanel.xaml` — 移除 local `Theme.xaml` merge（WPF 資源範圍遮蔽根因）
  - `PrintHeadController.xaml` / `SecuredButton.xaml` — ContentPresenter.Resources local implicit TextBlock Style，修正 Light 模式按鈕文字被 global implicit Style 覆蓋為深色的問題
  - `PrintHeadStatus.xaml` — Padding "8"→"4"、ClipToBounds="True"、Row1 Height="4"→"*"（StatusDataPanel 貼底，兩張卡視覺一致）
  - `DesignTimeControlFactory.cs` — StaticLabel / PlcStatusIndicator 改 SetResourceReference（主題動態響應）
  - `RuntimeControlFactory.cs`（DesignRuntime）— Spacer body 改 SetResourceReference("Surface.Bg.Card")、StaticLabel dynamic foreground、ColorThemeToResourceKey helper
  - `scripts/init-shell-app.ps1` — 同步以上三項修正 + Spacer headerColor 預設 "Primary"→"Normal"
  - `PropertyPanel.xaml` — StaticLabel 顏色改 ComboBox（配色系統）
  - `LightColors.xaml` — 補齊 Surface.* / Text.* / Action.* / Status.* token
- **下一步：**
    1. ⚠ 確認 MyPrintApp2 單獨重建後 Spacer header 灰色是否正確（需 rebuild MyPrintApp2.csproj，不是 Stackdose.UI.Core.sln）
    2. ⚠ 確認 PrintHead 2 高度在新佈局（Row1="*"）下視覺一致
    3. **PlcConfirmationHandler 實作** — 帶倒數功能的確認對話框
    4. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重載畫布

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `UI.Core/Shell/Handlers` | 帶倒數與 Event 接軌的確認框 |
| 2 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、direction + D512 flag 確認 |
| 3 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| **Spacer headerColor runtime 仍為 Primary 藍** | 高 | MyPrintApp2 有獨立 RuntimeControlFactory.cs 副本，需單獨 rebuild MyPrintApp2.csproj（非 Stackdose.UI.Core.sln）；代碼修正已完成 |
| **PrintHead 2 高度視覺問題** | 高 | Row1 改 Height="*" 後理論上 StatusDataPanel 貼底，需重建確認；config 載入失敗時無電壓行導致空白較多 |
| **D512 PLC flag 缺失** | 中 | ModelE 傳圖前寫 D512 作為層旗標，待實機確認是否需要補 |
| JSON 熱更新 | 中 | DesignRuntime 尚未實作 |

## 最近 Commits

```
[本次] fix: Light mode runtime — SecuredButton text, PrintHeadStatus layout, Spacer theme, AlarmViewer/Log/UserMgmt scope
[前次] fix: PlcLabel theme + Light surface gray + RuntimeFactory Spacer solid bg
aefd366 light模式處理未完成，PrintHeadStatus 跑版
6a456f6 fix: PrintHead disabled opacity + DesignTimeFactory Spacer solid bg
af218b9 fix: complete Light/Dark theme switching
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | ✅ 完成（含容器感知、全方位 Padding） |
| Light/Dark 主題切換 | ✅ 完成（Designer + DesignRuntime + 實機 App） |
