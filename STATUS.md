# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-06
- **分支：** `master`（UI.Core + Platform 皆已推送至 origin）
- **上次做了什麼：**
    - **Direction gap 修正：** `ConfigurePrintModeAsync` 補 `direction` 參數，ComboBox 擴充至 6 個方向選項（含 CarAtRight 系列），StartX/CaliMM 補入 log
    - **Dashboard 適螢幕：** scaffold 模板加 `□/❐` 放大/還原按鈕，`ViewBox Stretch=Uniform` 等比縮放，Window `Background="#1E1E32"` 修正 letterbox 白底；MyPrintApp 同步更新
- **下一步（明日上工順序）：**
    1. **ModelE 實機驗證** — 噴頭啟動接線與 M-bit 事件連動測試（direction + D512 flag 待實機確認）。
    2. **D512 PLC flag** — ModelE 傳圖前寫 D512 層旗標，確認新控件是否需要補上。
    3. **JSON 熱更新** — DesignRuntime 修改 JSON 後自動重新載入畫布。

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | 實機驗證與 wiring | — | 噴頭 init 與 Dashboard 反饋、direction + D512 flag 確認 |
| 2 | D512 PLC flag | `PrintHeadController.xaml.cs` | 待實機確認是否需要補 |
| 3 | JSON 熱更新 | `DesignRuntime` | 修改 JSON 後自動重載畫布 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| **D512 PLC flag 缺失** | 中 | ModelE 傳圖前寫 D512 作為層旗標，待實機確認是否需要補 |
| JSON 熱更新（修改 JSON 後自動重新載入畫布） | 中 | DesignRuntime 尚未實作 |

## 最近 Commits

```
06c1726 merge: Dashboard proportional maximize button  [2026-05-06]
892b600 feat: direction gap fix + devlog/status update  [2026-05-06]
feed0c8 chore: update FeiyangWrapper GUID and add MyPrintApp to solution  [2026-05-06]
4fb3519 refactor: SystemClock 搬移、Precision Gap 容器感知、PrintHeadController UI 重排  [2026-05-06]
```

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | 🟡 進行中（Padding 尚有小 Bug） |
