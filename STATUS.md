# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-06
- **分支：** `master`（UI.Core + Platform 皆已推送至 origin）
- **上次做了什麼：**
    - **PrintHeadController UI 重排：** 讀取/載入/取消三按鈕移至第二排（Grid `*` 等寬），`已連線` 狀態 Badge 移至 Header 右側；左側面板移除舊的圖片操作區塊。
    - **FeiyangWrapper 整合：** 將 WinForms 版 FeiyangWrapper.vcxproj 複製至 `D:\工作區\Project\Sdk\FeiyangWrapper\`；更新 `Stackdose.PrintHead.csproj` 為混合式（VS ProjectReference 建置順序 + DLL Reference 型別解析），修正 `dotnet build` 與 VS 兩種建置環境皆可通過的問題。
    - **Platform waveform 路徑：** `FeiyangPrintHead.cs` 新增 `ResolveWaveformPath()` 多路徑搜尋（Resources/ / Config/waves/ / Config/）。
    - **兩個 Repo 皆 commit + push（Platform: develop→master merge 完成）。**
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
feed0c8 chore: update FeiyangWrapper GUID and add MyPrintApp to solution  [2026-05-06]
4fb3519 refactor: SystemClock 搬移、Precision Gap 容器感知、PrintHeadController UI 重排  [2026-05-06]
338acdb merge: Platform develop→master (FeiyangWrapper ProjectReference + waveform fix)  [2026-05-06]
b7a07f8 docs: update STATUS.md and PROGRESS.md before session end  [2026-05-05]
```

> 今日未 commit：`PrintHeadController` direction gap fix（`IPrintHead` + `FeiyangPrintHead` + XAML 6 選項）

## 功能完成狀態快照

| 模組 | 完成度 |
|---|---|
| MachinePageDesigner | 23 / 23 ✅（含精密校正、堆疊工具、跨視窗剪貼） |
| DesignRuntime | 14 / 14 ✅（含尺寸補償） |
| 排版輔助系統 | 🟡 進行中（Padding 尚有小 Bug） |
