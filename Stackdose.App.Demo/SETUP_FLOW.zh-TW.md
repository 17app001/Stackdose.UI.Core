# Stackdose.App.Demo 從零到目前畫面的建置流程

本文件說明如何從頭建立 `Stackdose.App.Demo`，並完成目前畫面狀態：

- 使用 `MainContainer` 作為主殼層
- Header 裝置名稱可設定
- 主要內容頁為 `MachineOverviewPage`
- 左上顯示 PLC 狀態、右上顯示系統時間
- 底部 50/50 區塊
  - 左側：軟體資訊
  - 右側：`LiveLogViewer`
- Machine 卡片由 JSON 設定檔驅動

---

## 1. 建立專案並加入相依

在 solution 根目錄執行：

```bash
dotnet new wpf -n Stackdose.App.Demo -f net9.0-windows
dotnet sln "Stackdose.UI.Core.sln" add "Stackdose.App.Demo/Stackdose.App.Demo.csproj"
dotnet add "Stackdose.App.Demo/Stackdose.App.Demo.csproj" reference "Stackdose.UI.Core/Stackdose.UI.Core.csproj"
dotnet add "Stackdose.App.Demo/Stackdose.App.Demo.csproj" reference "Stackdose.UI.Templates/Stackdose.UI.Templates.csproj"
```

在 `Stackdose.App.Demo/Stackdose.App.Demo.csproj` 加入 JSON 複製規則：

```xml
<ItemGroup>
  <None Update="Config\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 2. 啟動時自動載入主題資源

共用 helper：

- `Stackdose.UI.Templates/Helpers/AppThemeBootstrapper.cs`

在 `Stackdose.App.Demo/App.xaml.cs`：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    AppThemeBootstrapper.Apply(this);
    base.OnStartup(e);
}
```

目的：避免 `Template.PanelCard` 等資源找不到的問題。

---

## 3. MainWindow 只放殼層 + 頁面

在 `MainWindow.xaml` 使用：

```xml
<Custom:MainContainer x:Name="MainShell"
                      IsShellMode="True"
                      HeaderDeviceName="DEMO"
                      CurrentMachineDisplayName=""
                      PageTitle="Machine Overview">
    <Custom:MainContainer.ShellContent>
        <TemplatePages:MachineOverviewPage />
    </Custom:MainContainer.ShellContent>
</Custom:MainContainer>
```

說明：

- `MainContainer` 已公開 `ShellContent`，可直接承載頁面
- `HeaderDeviceName` 對應 Header 左上裝置名稱

---

## 4. MainWindow.xaml.cs 瘦身

`MainWindow` 只做啟動轉發，不放商業邏輯：

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DemoRuntimeHost.Start(MainShell);
    }
}
```

---

## 5. 服務層拆分（重點）

目前拆分如下：

- `Services/DemoRuntimeHost.cs`
  - 啟動總調度
  - 載入 `app-meta.json` 與 `Machine*.config.json`
  - 套用到 `MainContainer` / `MachineOverviewPage`

- `Services/DemoConfigLoader.cs`
  - 載入機台設定檔

- `Services/DemoMonitorAddressBuilder.cs`
  - 組 PLC 監聽位址字串
  - 支援連續壓縮（例如 `M200,3`）

- `Services/DemoOverviewBinder.cs`
  - 把設定資料映射到 Overview 頁
  - 套 PLC 參數、Machine 卡片、app-meta 顯示設定

---

## 6. 機台設定檔（給非工程人員）

檔案：

- `Stackdose.App.Demo/Config/MachineA.config.json`
- `Stackdose.App.Demo/Config/MachineB.config.json`

內容包含：

- `machine`（id/name/enable）
- `plc`（ip/port/pollIntervalMs/autoConnect）
- `tags.status`
- `tags.process`

---

## 7. UI 顯示設定（app-meta）

檔案：

- `Stackdose.App.Demo/Config/app-meta.json`

可控制：

- Header 裝置名稱：`headerDeviceName`
- 頁面標題：`defaultPageTitle`
- 區塊顯示開關：
  - `showMachineCards`
  - `showSoftwareInfo`
  - `showLiveLog`
- 底部高度：`bottomPanelHeight`
- 左右標題：`bottomLeftTitle` / `bottomRightTitle`
- 左下資訊列表：`softwareInfoItems`

---

## 8. MachineOverviewPage 目前能力

已可配置項目：

- `ShowMachineCards`
- `ShowSoftwareInfo`
- `ShowLiveLog`
- `BottomPanelHeight`
- `BottomLeftTitle`
- `BottomRightTitle`
- `SoftwareInfoItems`

版面：

- 上方：標題 + PLC 狀態 + 時鐘
- 中間：Machine Cards
- 下方：50/50（左資訊 / 右 LiveLog）

---

## 9. 驗證指令

```bash
dotnet build "Stackdose.UI.Templates/Stackdose.UI.Templates.csproj" -c Release -warnaserror -m:1
dotnet build "Stackdose.App.Demo/Stackdose.App.Demo.csproj" -c Release -warnaserror -m:1
```

若偶發檔案鎖定（防毒或 `.NET Host`），重跑一次即可。

---

## 10. 建議下一步

做卡片點擊到明細頁：

- `MachineCard` 點擊 -> `MachineDetailPage`
- 同步更新 `CurrentMachineDisplayName`

接著可再把卡片欄位顯示順序抽成 layout JSON，達到更完整低程式碼流程。
