# Shell App 快速上手（3 步驟）

當你需要快速建立新的機台型 App，又不想先深入框架細節時，請先用這份文件。

## 0) 用 new-app.ps1 快速產生 JSON 驅動 App（推薦）

`new-app.ps1` 是 `init-shell-app.ps1 -JsonDrivenApp` 的薄包裝，支援三種模式：

```powershell
# SinglePage — 單頁設計稿執行（DesignRuntime 載入 .machinedesign.json）
powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "Stackdose.App.MyMonitor" -Mode SinglePage -DestinationRoot .

# Standard — 多頁 LeftNav + BehaviorEngine
powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "Stackdose.App.MyMonitor" -Mode Standard -DestinationRoot .

# Dashboard — 無邊框固定視窗，自動連線 PLC，適合機台旁看板
powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "Stackdose.App.MyMonitor" -Mode Dashboard -DestinationRoot .
```

Dashboard 模式的 App 特性：
- 視窗無標題列（`WindowStyle="None"`），大小自動符合設計稿畫布尺寸
- 底部細條：可拖動視窗 + IP:Port 連線燈號（綠=已連線 / 紅=未連線）+ X 關閉按鈕
- 啟動時自動讀取 `app-config.json` 並連線 PLC（`PlcIp` / `PlcPort` / `ScanInterval`）
- 在 MachinePageDesigner 選擇 `Layout: Dashboard` 後，會顯示 PLC IP / Port / ScanInterval 欄位

## 1) 產生新專案（舊式 init-shell-app.ps1）

```powershell
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourMachine" -DestinationRoot . -IncludeSecondDemoSampleConfigs
```

這會建立一個可執行的 WPF 專案，並包含 `Config/app-meta.json` 與選用的範例機台設定。

## 2) 先只改設定檔（最簡單）

先修改新專案下 `Config/` 資料夾中的檔案：

- `app-meta.json`
- `MachineA.config.json` / `MachineB.config.json`（若有包含）
- alarm/sensor 相關 json

如果你要切換 detail page 的綁定，且不想改 C#，直接在 `app-meta.json` 設定 `detailPage`。

機台設定檔的最小必要欄位（JSON-only 流程）：

- `machine.id`
- `machine.name`
- `machine.enable`
- `alarmConfigFile`
- `sensorConfigFile`
- `plc.ip` / `plc.port` / `plc.pollIntervalMs`
- `tags.status.isRunning` / `tags.status.isAlarm`
- `tags.process.batchNo` / `tags.process.recipeNo` / `tags.process.nozzleTemp`

在這個階段，除非你的機台需要客製映射邏輯，否則不要先改 adapter 程式碼。

## 3) 編譯與執行

```powershell
dotnet build .\Stackdose.App.YourMachine\Stackdose.App.YourMachine.csproj -c Debug
```

啟動後請確認：

- 選單與標題來自你的設定檔
- 機台卡片與監控數值正常載入
- 沒有異常 PLC 輪詢尖峰

進階整合請參考 `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md`。

單頁監控（Designer 為主）請參考 `Stackdose.App.SingleDetailLab/README_SINGLE_PAGE_QUICKSTART.md`。

## 單頁樣板產生方式

直接產生「單頁設計」起始專案：

```powershell
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourSinglePage" -DestinationRoot . -SinglePageDesigner
```

產生「專案本地可編輯頁面」版本（建議，用於各專案自行拖拉版面）：

```powershell
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourSinglePage" -DestinationRoot . -SinglePageDesignerLocalEditable
```

本地可編輯模式的版型選項：

```powershell
# 三欄（預設）
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourSinglePage" -DestinationRoot . -SinglePageDesignerLocalEditable -DesignerLayoutPreset ThreeColumn

# 兩欄（可自訂比例，範例 4:6）
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourSinglePage" -DestinationRoot . -SinglePageDesignerLocalEditable -DesignerLayoutPreset TwoColumn64 -DesignerSplitLeftWeight 4 -DesignerSplitRightWeight 6

# 2x2 區塊
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourSinglePage" -DestinationRoot . -SinglePageDesignerLocalEditable -DesignerLayoutPreset TwoByTwo
```

## 單頁預設行為（目前版本）

- 第 1 個區塊預設提供 `SecuredButton` 測試按鈕範例。
- 按鈕事件流程採用 `Page event relay -> MainWindowViewModel command`。
- 範例按鈕點擊會由 ViewModel 觸發 `CyberMessageBox`。
- `SensorViewer` / `AlarmViewer` 會依 `Machine1.config.json` 的 `sensorConfigFile` / `alarmConfigFile` 自動綁定。

若你在 `scripts/` 目錄內執行命令，請加上 `-DestinationRoot ..`，讓專案產生在 repo 根目錄。

## 用 CLI 產生器從 CSV Spec 建立專案

若有機台規格 CSV 檔，也可用 `Stackdose.Tools.ProjectGenerator` 從 spec 直接生成：

```powershell
# 從方案根目錄執行
dotnet run --project Stackdose.Tools.ProjectGenerator `
  -- --spec "Stackdose.App.DeviceFramework/docs/examples/MyOvenDemo-Spec.csv"
```
