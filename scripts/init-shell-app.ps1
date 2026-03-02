param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [string]$DestinationRoot = ".",

    [switch]$IncludeSecondDemoSampleConfigs,

    [switch]$SinglePageDesigner
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$From,
        [Parameter(Mandatory = $true)][string]$To
    )

    $fromResolved = (Resolve-Path $From).Path
    $toResolved = (Resolve-Path $To).Path

    $separator = [System.IO.Path]::DirectorySeparatorChar
    if (-not $fromResolved.EndsWith($separator)) {
        $fromResolved = "$fromResolved$separator"
    }

    $fromUri = New-Object System.Uri($fromResolved)
    $toUri = New-Object System.Uri($toResolved)
    $relative = $fromUri.MakeRelativeUri($toUri)
    return [System.Uri]::UnescapeDataString($relative.ToString()).Replace('/', $separator)
}

if ($AppName.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
    throw "AppName contains invalid file name characters: $AppName"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found in PATH. Install .NET SDK before running this script."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not (Test-Path $DestinationRoot)) {
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
}

$destinationRootFull = (Resolve-Path $DestinationRoot).Path
$projectDir = Join-Path $destinationRootFull $AppName

if (Test-Path $projectDir) {
    throw "Target project directory already exists: $projectDir"
}

Write-Host "[init-shell-app] Creating WPF project: $AppName"
dotnet new wpf -n $AppName -o $projectDir

if ($SinglePageDesigner) {
    $projectFile = Join-Path $projectDir "$AppName.csproj"
    if (-not (Test-Path $projectFile)) {
        throw "Project file not found: $projectFile"
    }

    [xml]$csprojXml = Get-Content -Path $projectFile -Raw
    $projectNode = $csprojXml.Project

    $uiCoreRef = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Core\Stackdose.UI.Core.csproj")
    $templatesRef = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Templates\Stackdose.UI.Templates.csproj")

    $referenceGroup = $csprojXml.SelectSingleNode('/Project/ItemGroup[ProjectReference]')
    if ($null -eq $referenceGroup) {
        $referenceGroup = $csprojXml.CreateElement("ItemGroup")
        $projectNode.AppendChild($referenceGroup) | Out-Null
    }

    $coreRefNode = $csprojXml.CreateElement("ProjectReference")
    $coreRefNode.SetAttribute("Include", $uiCoreRef)
    $referenceGroup.AppendChild($coreRefNode) | Out-Null

    $templatesRefNode = $csprojXml.CreateElement("ProjectReference")
    $templatesRefNode.SetAttribute("Include", $templatesRef)
    $referenceGroup.AppendChild($templatesRefNode) | Out-Null

    $csprojXml.Save($projectFile)

    @"
using Stackdose.UI.Templates.Helpers;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Windows;

namespace $AppName;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppThemeBootstrapper.Apply(this);
        SecurityContext.QuickLogin(AccessLevel.SuperAdmin);
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
"@ | Set-Content -Path (Join-Path $projectDir "App.xaml.cs") -Encoding UTF8

    @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:Templates="http://schemas.stackdose.com/templates"
        mc:Ignorable="d"
        Title="Single Detail Designer"
        Height="900"
        Width="1600"
        WindowState="Maximized"
        WindowStyle="None"
        ResizeMode="CanResize">
    <Templates:SinglePageContainer x:Name="MainShell"
                                   HeaderDeviceName="SINGLE-DESIGNER"
                                   PageTitle="Single Detail Designer"
                                   CloseRequested="MainShell_OnCloseRequested"
                                   MinimizeRequested="MainShell_OnMinimizeRequested"
                                   LogoutRequested="MainShell_OnLogoutRequested">
        <Templates:SinglePageContainer.ShellContent>
            <Templates:SingleDetailWorkspacePage />
        </Templates:SinglePageContainer.ShellContent>
    </Templates:SinglePageContainer>
</Window>
"@ | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml") -Encoding UTF8

    @"
using System.IO;
using System.Text.Json;
using System.Windows;
using Stackdose.UI.Templates.Pages;

namespace $AppName;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Config", "Machine1.config.json");
        if (!File.Exists(configPath))
        {
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = doc.RootElement;

        var machine = root.GetProperty("machine");
        var plc = root.GetProperty("plc");
        var machineName = machine.GetProperty("name").GetString() ?? "Machine";
        var machineId = machine.GetProperty("id").GetString() ?? "M1";
        var ip = plc.GetProperty("ip").GetString() ?? "127.0.0.1";
        var port = plc.GetProperty("port").GetInt32();
        var interval = plc.GetProperty("pollIntervalMs").GetInt32();
        var autoConnect = !plc.TryGetProperty("autoConnect", out var autoElement) || autoElement.GetBoolean();

        if (MainShell.ShellContent is SingleDetailWorkspacePage page)
        {
            page.Initialize(machineName, machineId, ip, port, interval, autoConnect, "D400,40");
        }

        MainShell.HeaderDeviceName = machineName;
        MainShell.PageTitle = $"{machineName} - Single Detail";
    }

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e) => Close();
    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e) => WindowState = WindowState.Minimized;
    private void MainShell_OnCloseRequested(object? sender, EventArgs e) => Close();
}
"@ | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml.cs") -Encoding UTF8
}

$configDir = Join-Path $projectDir "Config"
New-Item -ItemType Directory -Path $configDir | Out-Null

$appMetaTemplate = Join-Path $repoRoot "Stackdose.UI.Core\Shell\app-meta.template.json"
$appMetaTarget = Join-Path $configDir "app-meta.json"
if (-not (Test-Path $appMetaTemplate)) {
    throw "App meta template not found: $appMetaTemplate"
}

Copy-Item $appMetaTemplate $appMetaTarget -Force

if ($IncludeSecondDemoSampleConfigs) {
    $sampleConfigDir = Join-Path $repoRoot "Stackdose.App.SecondDemo\Config"
    if (Test-Path $sampleConfigDir) {
        Copy-Item (Join-Path $sampleConfigDir "MachineA.config.json") (Join-Path $configDir "MachineA.config.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.config.json") (Join-Path $configDir "MachineB.config.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineA.alarms.json") (Join-Path $configDir "MachineA.alarms.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.alarms.json") (Join-Path $configDir "MachineB.alarms.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineA.sensors.json") (Join-Path $configDir "MachineA.sensors.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.sensors.json") (Join-Path $configDir "MachineB.sensors.json") -Force
    } else {
        Write-Warning "Sample config directory not found, skipping sample config copy: $sampleConfigDir"
    }
} else {
    @"
{
  "machine": {
    "id": "M1",
    "name": "Machine 01",
    "enable": true
  },
  "alarmConfigFile": "Config/Machine1.alarms.json",
  "sensorConfigFile": "Config/Machine1.sensors.json",
  "plc": {
    "ip": "127.0.0.1",
    "port": 5000,
    "pollIntervalMs": 150,
    "autoConnect": true
  },
  "tags": {
    "status": {
      "isRunning": { "address": "M201", "type": "bool", "access": "read" },
      "isAlarm": { "address": "M202", "type": "bool", "access": "read" }
    },
    "process": {
      "batchNo": { "address": "D400", "type": "string", "access": "read", "length": 8 },
      "recipeNo": { "address": "D410", "type": "string", "access": "read", "length": 8 },
      "nozzleTemp": { "address": "D420", "type": "int16", "access": "read" }
    }
  }
}
"@ | Set-Content -Path (Join-Path $configDir "Machine1.config.json") -Encoding UTF8

    @"
{
  "Alarms": [
    { "Device": "M202", "Bit": 0, "Label": "General Alarm" }
  ]
}
"@ | Set-Content -Path (Join-Path $configDir "Machine1.alarms.json") -Encoding UTF8

    @"
[
  { "Device": "D420", "Name": "Nozzle Temp" }
]
"@ | Set-Content -Path (Join-Path $configDir "Machine1.sensors.json") -Encoding UTF8
}

$readmePath = Join-Path $projectDir "SHELL_QUICKSTART.md"
if ($SinglePageDesigner) {
    @"
# Shell Quickstart (Single Page Designer)

1. Edit `Config/Machine1.config.json` for PLC connection and addresses.
2. Open `MainWindow.xaml` + `Templates:SingleDetailWorkspacePage` in Visual Studio designer.
3. Drag `UI.Core` controls into Group A/B/C and run.

Reference:
- Repo root `Stackdose.App.SingleDetailLab/README_SINGLE_PAGE_QUICKSTART.md`
"@ | Set-Content -Path $readmePath -Encoding UTF8
} else {
    @"
# Shell Quickstart

1. Configure your app in `Config/app-meta.json`.
2. Update machine/alarm/sensor json files under `Config/`.
   - required keys in machine config: alarmConfigFile, sensorConfigFile
3. Build and run your project.

Reference:
- Repo root `QUICKSTART.md` (recommended)
- `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md` (advanced wiring details)
"@ | Set-Content -Path $readmePath -Encoding UTF8
}

Write-Host "[init-shell-app] Done. Generated: $projectDir"
