param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [string]$DestinationRoot = ".",

    [switch]$IncludeSecondDemoSampleConfigs,

    [switch]$SinglePageDesigner,

    [switch]$SinglePageDesignerLocalEditable
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

$projectFile = Join-Path $projectDir "$AppName.csproj"
if (-not (Test-Path $projectFile)) {
    throw "Project file not found: $projectFile"
}

[xml]$baseCsprojXml = Get-Content -Path $projectFile -Raw
$baseProjectNode = $baseCsprojXml.Project
$configCopyNode = $baseCsprojXml.SelectSingleNode('/Project/ItemGroup/None[@Update="Config\\*.json"]')
if ($null -eq $configCopyNode) {
    $copyGroup = $baseCsprojXml.CreateElement("ItemGroup")
    $noneNode = $baseCsprojXml.CreateElement("None")
    $noneNode.SetAttribute("Update", "Config\*.json")
    $copyNode = $baseCsprojXml.CreateElement("CopyToOutputDirectory")
    $copyNode.InnerText = "PreserveNewest"
    $noneNode.AppendChild($copyNode) | Out-Null
    $copyGroup.AppendChild($noneNode) | Out-Null
    $baseProjectNode.AppendChild($copyGroup) | Out-Null
    $baseCsprojXml.Save($projectFile)
}

$singlePageMode = $SinglePageDesigner -or $SinglePageDesignerLocalEditable

if ($singlePageMode) {
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
<Application x:Class="$AppName.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>

    </Application.Resources>
</Application>
"@ | Set-Content -Path (Join-Path $projectDir "App.xaml") -Encoding UTF8

    if ($SinglePageDesignerLocalEditable) {
        @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:Templates="http://schemas.stackdose.com/templates"
        xmlns:pages="clr-namespace:$AppName.Pages"
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
            <pages:SingleDetailWorkspacePage />
        </Templates:SinglePageContainer.ShellContent>
    </Templates:SinglePageContainer>
</Window>
"@ | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml") -Encoding UTF8

        $pagesDir = Join-Path $projectDir "Pages"
        New-Item -ItemType Directory -Path $pagesDir -Force | Out-Null

        @"
<UserControl x:Class="$AppName.Pages.SingleDetailWorkspacePage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:core="http://schemas.stackdose.com/wpf"
             xmlns:templateControls="clr-namespace:Stackdose.UI.Templates.Controls;assembly=Stackdose.UI.Templates"
             mc:Ignorable="d"
             d:DesignHeight="900"
             d:DesignWidth="1400">

    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Grid Background="{DynamicResource Surface.Bg.Page}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Grid.Row="0"
                Background="{DynamicResource Surface.Bg.Panel}"
                BorderBrush="{DynamicResource Surface.Border.Default}"
                BorderThickness="1"
                CornerRadius="8"
                Padding="12"
                Margin="0,0,0,12">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <StackPanel Grid.Row="0">
                    <TextBlock Text="Detail + PLC Lab"
                               FontSize="20"
                               FontWeight="SemiBold"
                               Foreground="{DynamicResource TextPrimaryBrush}"/>
                    <TextBlock x:Name="MachineSummaryText"
                               Margin="0,4,0,10"
                               Foreground="{DynamicResource TextSecondaryBrush}"/>
                </StackPanel>

                <Grid Grid.Row="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="1*" />
                        <ColumnDefinition Width="3*" />
                    </Grid.ColumnDefinitions>

                    <core:PlcStatus x:Name="TopPlcStatus"
                                    Grid.Column="0"
                                    Height="58"
                                    IsGlobal="True"
                                    ShowBorder="True"
                                    AutoConnect="False"/>

                    <Border Grid.Column="1" Background="Transparent"/>
                </Grid>
            </Grid>
        </Border>

        <Border Grid.Row="1"
                Background="{DynamicResource Surface.Bg.Panel}"
                BorderBrush="{DynamicResource Surface.Border.Default}"
                BorderThickness="1"
                CornerRadius="8"
                Padding="12">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row="0"
                           Text="Design this area in Visual Studio Designer."
                           Margin="0,0,0,12"
                           Foreground="{DynamicResource TextSecondaryBrush}"/>

                <Grid Grid.Row="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="12" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="12" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>

                    <templateControls:GroupBoxBlock Grid.Column="0" Header="Control Group A" BadgeText="Primary" GroupPadding="10" />
                    <templateControls:GroupBoxBlock Grid.Column="2" Header="Control Group B" BadgeText="Drop Here" GroupPadding="10" />
                    <templateControls:GroupBoxBlock Grid.Column="4" Header="Control Group C" BadgeText="Drop Here" GroupPadding="10" />
                </Grid>
            </Grid>
        </Border>
    </Grid>
</UserControl>
"@ | Set-Content -Path (Join-Path $pagesDir "SingleDetailWorkspacePage.xaml") -Encoding UTF8

        @"
using System.Windows.Controls;

namespace $AppName.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(string machineName, string machineId, string plcIp, int plcPort, int scanIntervalMs, bool autoConnect, string monitorAddress)
    {
        MachineSummaryText.Text = $"Machine: {machineName} ({machineId})";
        TopPlcStatus.IpAddress = plcIp;
        TopPlcStatus.Port = plcPort;
        TopPlcStatus.ScanInterval = scanIntervalMs;
        TopPlcStatus.AutoConnect = autoConnect;
        TopPlcStatus.MonitorAddress = monitorAddress;
    }
}
"@ | Set-Content -Path (Join-Path $pagesDir "SingleDetailWorkspacePage.xaml.cs") -Encoding UTF8
    } else {
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
    }

    $workspaceUsing = if ($SinglePageDesignerLocalEditable) { "using $AppName.Pages;" } else { "" }
    $workspaceType = if ($SinglePageDesignerLocalEditable) { "SingleDetailWorkspacePage" } else { "Stackdose.UI.Templates.Pages.SingleDetailWorkspacePage" }

@"
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
$workspaceUsing

namespace $AppName;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainShell.MinimizeCommand = new ActionCommand(_ => WindowState = WindowState.Minimized);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configPath = ResolveConfigPath();
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
        var monitorAddress = BuildMonitorAddress(root);

        if (MainShell.ShellContent is $workspaceType page)
        {
            page.Initialize(machineName, machineId, ip, port, interval, autoConnect, monitorAddress);
        }

        MainShell.HeaderDeviceName = machineName;
        MainShell.PageTitle = $"{machineName} - Single Detail";
    }

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e) => Close();
    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e) => WindowState = WindowState.Minimized;
    private void MainShell_OnCloseRequested(object? sender, EventArgs e) => Close();

    private static string ResolveConfigPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            var projectConfig = Path.Combine(current.FullName, "$AppName", "Config", "Machine1.config.json");
            if (File.Exists(projectConfig))
            {
                return projectConfig;
            }

            current = current.Parent;
        }

        var outputConfig = Path.Combine(AppContext.BaseDirectory, "Config", "Machine1.config.json");
        if (File.Exists(outputConfig))
        {
            return outputConfig;
        }

        return outputConfig;
    }

    private static string BuildMonitorAddress(JsonElement root)
    {
        if (!root.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
        {
            return "D400,40";
        }

        var expanded = new List<(string Prefix, int Number, string Raw)>();
        AddSectionAddresses(tags, "status", expanded);
        AddSectionAddresses(tags, "process", expanded);

        if (expanded.Count == 0)
        {
            return "D400,40";
        }

        var sorted = expanded
            .Distinct()
            .OrderBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Number)
            .ToList();

        var groups = new List<string>();
        var i = 0;
        while (i < sorted.Count)
        {
            var start = sorted[i];
            var end = i;

            while (end + 1 < sorted.Count
                   && string.Equals(sorted[end + 1].Prefix, start.Prefix, StringComparison.OrdinalIgnoreCase)
                   && sorted[end + 1].Number == sorted[end].Number + 1)
            {
                end++;
            }

            var length = end - i + 1;
            groups.Add(length > 1 ? $"{start.Raw},{length}" : start.Raw);
            i = end + 1;
        }

        return string.Join(',', groups);
    }

    private static void AddSectionAddresses(JsonElement tags, string sectionName, List<(string Prefix, int Number, string Raw)> result)
    {
        if (!tags.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var tagEntry in section.EnumerateObject())
        {
            var tag = tagEntry.Value;
            if (tag.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (tag.TryGetProperty("access", out var accessElement)
                && accessElement.ValueKind == JsonValueKind.String
                && !string.Equals(accessElement.GetString(), "read", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!tag.TryGetProperty("address", out var addressElement) || addressElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var address = addressElement.GetString();
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var baseAddress = ParseAddress(address);
            if (baseAddress == null)
            {
                continue;
            }

            var length = 1;
            if (tag.TryGetProperty("type", out var typeElement)
                && typeElement.ValueKind == JsonValueKind.String
                && string.Equals(typeElement.GetString(), "string", StringComparison.OrdinalIgnoreCase)
                && tag.TryGetProperty("length", out var lengthElement)
                && lengthElement.TryGetInt32(out var configuredLength))
            {
                length = Math.Max(1, configuredLength);
            }

            for (var i = 0; i < length; i++)
            {
                var next = (baseAddress.Value.Prefix, baseAddress.Value.Number + i, $"{baseAddress.Value.Prefix}{baseAddress.Value.Number + i}");
                result.Add(next);
            }
        }
    }

    private static (string Prefix, int Number)? ParseAddress(string address)
    {
        var match = Regex.Match(address.Trim(), "^([A-Za-z]+)(\\d+)$");
        if (!match.Success)
        {
            return null;
        }

        if (!int.TryParse(match.Groups[2].Value, out var number))
        {
            return null;
        }

        return (match.Groups[1].Value.ToUpperInvariant(), number);
    }

    private sealed class ActionCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public ActionCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
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
    "ip": "192.168.22.39",
    "port": 3000,
    "pollIntervalMs": 150,
    "autoConnect": false
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
if ($singlePageMode) {
    $singlePageModeTitle = if ($SinglePageDesignerLocalEditable) { "Single Page Designer (Local Editable Page)" } else { "Single Page Designer" }

    @"
# Shell Quickstart ($singlePageModeTitle)

1. Edit `Config/Machine1.config.json` for PLC connection and addresses.
2. Open the designer page in Visual Studio:
   - local editable mode: `Pages/SingleDetailWorkspacePage.xaml`
   - template mode: `Templates:SingleDetailWorkspacePage` inside `MainWindow.xaml`
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
