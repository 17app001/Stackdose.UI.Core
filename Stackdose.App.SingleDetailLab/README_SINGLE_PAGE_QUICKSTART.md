# Single Page Lab Quickstart

這份說明給設計師與開發者快速上手 `Stackdose.App.SingleDetailLab`。

目標：

- 單頁監控畫面（無左側功能選單）
- 首頁即 Detail 工作區
- 上方 `PlcStatus`，下方可自由拖拉與排版 UI 控件（在 Visual Studio 設計器）

## 1) 先跑起來

```powershell
dotnet build .\Stackdose.App.SingleDetailLab\Stackdose.App.SingleDetailLab.csproj -c Debug
```

啟動後會直接進入單頁畫面，且預設已 `SuperAdmin` 登入（方便測試 `SecuredButton`/權限控件）。

## 2) 改 PLC 連線與監控來源

編輯：`Stackdose.App.SingleDetailLab/Config/MachineA.config.json`

至少要確認：

- `plc.ip`
- `plc.port`
- `plc.pollIntervalMs`
- `tags.status.*` / `tags.process.*` 位址正確

`PlcStatus` 會在連線後註冊 monitor 範圍，`PlcLabel` 等控件從 monitor 快取更新，不是每個控件自己直讀 PLC。

## 3) 在 Visual Studio 設計器做版面

主要編排檔案（目前由 Templates 提供）：

- `Stackdose.UI.Templates/Pages/SingleDetailWorkspacePage.xaml`

操作方式：

1. 開啟 `SingleDetailWorkspacePage.xaml`（Design 視圖）
2. 在 `Control Group A / B / C` 放入你要的控件
3. 常用控件：`PlcLabel`, `PlcDeviceEditor`, `LiveLogViewer`, `SecuredButton`
4. 儲存 XAML 後直接執行查看

說明：

- 上方 `PlcStatus` 版面已固定 25% 寬，右側預留空白
- 下方三個 `GroupBoxBlock` 是預設分組骨架，可自由改名、增減、重排

若你想要「每個專案獨立版面」，可把 `SingleDetailWorkspacePage` 複製到 app 專案內再客製，不影響其他專案。

## 4) 推薦控件放置規則（讓團隊易維護）

- Group A：核心即時狀態（Batch/Recipe/Running/Alarm）
- Group B：操作區（Editor/Command/按鈕）
- Group C：監控與輔助（Log/診斷/備用）

## 5) 常見問題

- `PlcLabel` 沒更新：先確認 `PlcStatus` 是否顯示 `CONNECTED`
- 值顯示為空：檢查控件 `Address` 與 config 的實際位址
- 權限控件看不到：此專案已預設 SuperAdmin，若仍有問題請檢查控件 `RequiredLevel`

## 6) 重要邊界

- 目前只調整 `SingleDetailLab` 專案層
- 不修改 `UI.Core` 控件邏輯（除非先討論並確認）
