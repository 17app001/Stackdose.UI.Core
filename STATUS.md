# 專案狀態

> 每次任務結束後更新。這是唯一的動態狀態來源。

## 現況

- **日期：** 2026-05-04
- **分支：** `master`
- **上次做了什麼：**
    - **ModelE 移植啟動完成：** Scaffold 建立、57 筆 alarm / 10 筆 sensor 轉換、machinedesign.json 雙噴頭+四軸+12按鈕佈局、feiyang_head2.json 建立、build 0 錯誤。
    - **軸位址確認：** 從 WinForms Form1.cs 查明 X_A=D65、X_B=D67、Z_A=D69、Z_B=D71（DWord 32-bit），machinedesign.json 已更正。
    - **MachineStateHandler 範本：** `ModelE/Handlers/MachineStateHandler.cs` 建立，示範 isEnabled/label/visibility prop 控制；JSON events 用法已寫在註解。
    - **scaffold bug 修正：** `init-shell-app.ps1` 的 vcxproj ProjectReference 補上 MSBuildRuntimeType 條件。
- **下一步（明日上工順序）：**
    1. `PlcConfirmationHandler` — 倒數確認 Dialog
    2. 噴頭啟動 wiring — MainWindow OnLoaded 串接
    3. machinedesign.json 補 M9/M1/M4 監控 events

## 進行中

| # | 任務 | 檔案 | 備註 |
|---|---|---|---|
| 1 | PlcConfirmationHandler | `ModelE/Handlers/PlcConfirmationHandler.cs` | 對應 WinForms PlcMessageForm，倒數→寫回 ConfirmAddress |
| 2 | 噴頭啟動 wiring | `ModelE/MainWindow.xaml.cs` | ConnectionEstablished 事件後 init PrintHead |
| 3 | machinedesign.json M-bit events | `ModelE/Config/M1.machinedesign.json` | M9=列印中→停用btnPrint；M1/M4=初始化中→停用相關按鈕 |
| 4 | 實機驗證 | — | feiyang_head2 BoardIP、app-config PLC IP、wave 檔 |
| 5 | D2000 alarm 確認 | `Machine1.alarms.json` | 循環系統_A 供墨超時 用 D2000 還是 D900，需對 PLC 程式 |
| 6 | 大檔案傳圖壓力測試 | — | 進度條已實作，尚未大檔案驗證 |

## ⚠️ 未解問題

| 問題 | 優先度 | 備註 |
|---|---|---|
| ProcessStatusIndicator / PlcDeviceEditor DesignRuntime 渲染未實機驗證 | Low | 程式碼已加，邏輯未跑過 |

## 最近 Commits

```
8dd306c 完成更新動作（GroupBox/Viewer 視覺同步、Scaffold 強化）
789dee4 fix: scaffold creates sample alarm/sensor configs + GroupBox header contrast
7012e80 fix: improve group header visibility in AlarmViewer and SensorViewer
6aa3b55 fix: use case-insensitive JSON deserialization in AlarmViewer
6949117 fix: correct ButtonTheme namespace (Models not Controls) in scaffold
1979273 fix: apply ButtonTheme prop in scaffold CreateSecuredButton
```

## 功能完成狀態快照（2026-05-04）

| 模組 | 完成度 |
|---|---|
| 核心框架 PLC / 日誌 / 權限 | 11 / 11 ✅ |
| MachinePageDesigner | 19 / 19 ✅（含 TabPanel + 子項設計器） |
| DesignRuntime + DesignViewer | 12 / 12 ✅ |
| 開發工具（ProjectGenerator）| 6 / 6 ✅ |
| PrintHead 整合 | ✅ 完成 |
| Dashboard 模式 | ✅ 完成（2026-04-23） |
| 底層重構 B0–B9 | ✅ 完成（2026-04-21~23） |
| TabPanel 控件 + 子項設計器 | ✅ 完成（2026-05-04） |
| Scaffold AutoFullPack | ✅ 完成（2026-05-04） |
| GroupBox / Viewer 視覺對齊 | ✅ 完成（2026-05-04） |
| ModelE 移植 — 靜態層（JSON/Config） | ✅ 完成（2026-05-04） |
| ModelE 移植 — 邏輯層（Handlers/Events） | 🔄 進行中 |
