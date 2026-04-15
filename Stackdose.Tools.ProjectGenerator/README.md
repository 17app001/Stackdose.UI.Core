# Stackdose.Tools.ProjectGenerator

**CSV 規格轉 WPF 專案的 CLI 工具**。讀取機台規格 CSV，自動產生完整可編譯的 DeviceFramework App 專案，包含 `csproj`、`Config/*.json`、`CommandHandlers.cs` 等所有骨架檔案。

## 這是什麼

CLI 函式庫 + 執行入口，搭配 PowerShell 腳本（`scripts/init-shell-app.ps1`）或直接 `dotnet run` 使用。

## 依賴

- 無外部依賴（純 .NET 8 Console App）

## 使用方式

```powershell
# 從方案根目錄執行
dotnet run --project Stackdose.Tools.ProjectGenerator `
  -- --spec "Stackdose.App.DeviceFramework/docs/examples/MyOvenDemo-Spec.csv"

# 指定輸出目錄
dotnet run --project Stackdose.Tools.ProjectGenerator `
  -- --spec "path/to/Spec.csv" --output "D:\Projects"
```

## CSV 規格格式

CSV 分區塊設定：

| 區塊 | 說明 |
|---|---|
| `[Project]` | 專案名稱、HeaderDeviceName、Version、PageMode |
| `[Machine]` | MachineId、MachineName、PLC IP/Port |
| `[Commands]` | MachineId、CommandName（Start/Pause/Stop 等） |
| `[Labels]` | MachineId、LabelKey、PLC 位址 |
| `[Panels]` | 附加面板（Settings、MaintenanceMode） |

範例規格檔位於 `Stackdose.App.DeviceFramework/docs/examples/`。

## 核心類別

| 類別 | 職責 |
|---|---|
| `Program` | CLI 入口，參數解析 + 進度輸出 |
| `CsvParser` | 解析規格 CSV 為 `DeviceSpec` |
| `DeviceSpec` | 規格資料模型（Project / Machines / Commands / Labels） |
| `ProjectGenerator` | 依規格產生所有目標檔案 |

## 產生的檔案

- `ProjectName.csproj`
- `App.xaml` / `App.xaml.cs`
- `MainWindow.xaml` / `MainWindow.xaml.cs`
- `Config/app-meta.json`
- `Config/Machine*.config.json`（每台機台一個）
- `Handlers/CommandHandlers.cs`（填入業務邏輯的主要擴充點）
- （可選）`Pages/SettingsPage.xaml`、`Pages/MaintenancePage.xaml`
