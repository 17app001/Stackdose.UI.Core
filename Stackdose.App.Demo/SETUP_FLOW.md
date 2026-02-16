# Stackdose App Demo Setup Flow (From Zero to Current UI)

This guide explains how to build `Stackdose.App.Demo` from scratch and reach the current screen state:

- Main shell via `MainContainer`
- Header device name configurable
- `MachineOverviewPage` as main content
- PLC status block on top-left
- System clock on top-right
- Bottom area 50/50 layout:
  - left: software info block
  - right: live log viewer
- Machine cards loaded from JSON config

---

## 1) Create the project and wire references

From solution root:

```bash
dotnet new wpf -n Stackdose.App.Demo -f net9.0-windows
dotnet sln "Stackdose.UI.Core.sln" add "Stackdose.App.Demo/Stackdose.App.Demo.csproj"
dotnet add "Stackdose.App.Demo/Stackdose.App.Demo.csproj" reference "Stackdose.UI.Core/Stackdose.UI.Core.csproj"
dotnet add "Stackdose.App.Demo/Stackdose.App.Demo.csproj" reference "Stackdose.UI.Templates/Stackdose.UI.Templates.csproj"
```

In `Stackdose.App.Demo/Stackdose.App.Demo.csproj`, ensure JSON config files copy to output:

```xml
<ItemGroup>
  <None Update="Config\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 2) Enable theme/resource bootstrap automatically

The reusable bootstrap helper is in:

- `Stackdose.UI.Templates/Helpers/AppThemeBootstrapper.cs`

`Stackdose.App.Demo/App.xaml.cs` should call it on startup:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    AppThemeBootstrapper.Apply(this);
    base.OnStartup(e);
}
```

This avoids common runtime resource failures (missing `Template.PanelCard`, etc.).

---

## 3) Build the shell in MainWindow.xaml

Use `MainContainer` directly and set shell-level properties:

- `IsShellMode`
- `HeaderDeviceName`
- `CurrentMachineDisplayName`
- `PageTitle`
- `ShellContent`

Current pattern (already used):

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

Notes:

- `MachineOverviewPage` uses `TemplatePages` namespace mapping.
- `MainContainer` now exposes `ShellContent`, so content pages can be dropped in XAML.

---

## 4) Keep MainWindow.xaml.cs thin (host only)

Do not keep machine/parsing logic in `MainWindow`.

Current `MainWindow` pattern:

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

All runtime logic is moved to services.

---

## 5) Put runtime logic in services (no UI code-behind bloat)

Current split:

- `Services/DemoRuntimeHost.cs`:
  - startup orchestration
  - load app meta + machine configs
  - apply to shell and overview page

- `Services/DemoConfigLoader.cs`:
  - load `Machine*.config.json`

- `Services/DemoMonitorAddressBuilder.cs`:
  - build PLC monitor address string
  - includes contiguous merge (example `M200,3`)

- `Services/DemoOverviewBinder.cs`:
  - map config data to `MachineOverviewPage`
  - set PLC connection fields
  - set machine cards
  - apply app-meta options

---

## 6) Configure machine data (non-developer editable)

Machine files:

- `Stackdose.App.Demo/Config/MachineA.config.json`
- `Stackdose.App.Demo/Config/MachineB.config.json`

Each file defines:

- `machine`
- `plc` (`ip`, `port`, `pollIntervalMs`, `autoConnect`)
- `tags.status`
- `tags.process`

This drives cards + PLC monitor addresses.

---

## 7) Configure UI meta (non-developer editable)

Meta file:

- `Stackdose.App.Demo/Config/app-meta.json`

This controls UI behavior without code changes:

- header label (`headerDeviceName`)
- page title (`defaultPageTitle`)
- show/hide sections
  - `showMachineCards`
  - `showSoftwareInfo`
  - `showLiveLog`
- bottom panel height (`bottomPanelHeight`)
- bottom section titles
  - `bottomLeftTitle`
  - `bottomRightTitle`
- software info rows (`softwareInfoItems`)

---

## 8) What changed in MachineOverviewPage for reuse

`MachineOverviewPage` now supports configurable layout/state:

- top area: PLC + clock
- cards area: optional
- bottom area: 50/50 left-right
  - left: software info list
  - right: live log

Important page properties:

- `ShowMachineCards`
- `ShowSoftwareInfo`
- `ShowLiveLog`
- `BottomPanelHeight`
- `BottomLeftTitle`
- `BottomRightTitle`
- `SoftwareInfoItems`

---

## 9) Build and verify

```bash
dotnet build "Stackdose.UI.Templates/Stackdose.UI.Templates.csproj" -c Release -warnaserror -m:1
dotnet build "Stackdose.App.Demo/Stackdose.App.Demo.csproj" -c Release -warnaserror -m:1
```

If you see occasional file-lock errors from antivirus or `.NET Host`, rerun once.

---

## 10) Recommended next step

Add page navigation flow:

- click `MachineCard` -> switch to `MachineDetailPage`
- set `CurrentMachineDisplayName` based on selected machine

Then move card field mapping to a new JSON layout file for full low-code control.
