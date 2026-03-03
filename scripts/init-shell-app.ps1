param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [string]$DestinationRoot = ".",

    [switch]$IncludeSecondDemoSampleConfigs,

    [switch]$SinglePageDesigner,

    [switch]$SinglePageDesignerLocalEditable,

    [ValidateSet("ThreeColumn", "TwoColumn64", "TwoByTwo")]
    [string]$DesignerLayoutPreset = "ThreeColumn",

    [ValidateRange(1, 20)]
    [int]$DesignerSplitLeftWeight = 3,

    [ValidateRange(1, 20)]
    [int]$DesignerSplitRightWeight = 2
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

$leftRatioPercent = [Math]::Round(($DesignerSplitLeftWeight * 100.0) / ($DesignerSplitLeftWeight + $DesignerSplitRightWeight))
$rightRatioPercent = 100 - $leftRatioPercent

$designerLayoutMarkup = switch ($DesignerLayoutPreset) {
    "TwoColumn64" {
@"
                <Grid Grid.Row="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="$($DesignerSplitLeftWeight)*" />
                        <ColumnDefinition Width="12" />
                        <ColumnDefinition Width="$($DesignerSplitRightWeight)*" />
                    </Grid.ColumnDefinitions>

                    <templateControls:GroupBoxBlock Grid.Column="0" Header="Control Group A" BadgeText="$($leftRatioPercent)%" GroupPadding="10" />
                    <templateControls:GroupBoxBlock Grid.Column="2" Header="Control Group B" BadgeText="$($rightRatioPercent)%" GroupPadding="10" />
                </Grid>
"@
    }
    "TwoByTwo" {
@"
                <Grid Grid.Row="1">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="*" />
                        <RowDefinition Height="12" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <Grid Grid.Row="0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="12" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <templateControls:GroupBoxBlock Grid.Column="0" Header="Top Left" BadgeText="Group A" GroupPadding="10" />
                        <templateControls:GroupBoxBlock Grid.Column="2" Header="Top Right" BadgeText="Group B" GroupPadding="10" />
                    </Grid>
                    <Grid Grid.Row="2">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="12" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <templateControls:GroupBoxBlock Grid.Column="0" Header="Bottom Left" BadgeText="Group C" GroupPadding="10" />
                        <templateControls:GroupBoxBlock Grid.Column="2" Header="Bottom Right" BadgeText="Group D" GroupPadding="10" />
                    </Grid>
                </Grid>
"@
    }
    default {
@"
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
"@
    }
}

if ($singlePageMode) {
    if (-not $SinglePageDesignerLocalEditable -and $DesignerLayoutPreset -ne "ThreeColumn") {
        Write-Warning "DesignerLayoutPreset applies to -SinglePageDesignerLocalEditable. Template mode keeps default layout."
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
                                   HeaderDeviceName="{Binding HeaderDeviceName}"
                                   PageTitle="{Binding PageTitle}"
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

$designerLayoutMarkup
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
                                   HeaderDeviceName="{Binding HeaderDeviceName}"
                                   PageTitle="{Binding PageTitle}"
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
using System.Windows;
$workspaceUsing
using $AppName.Services;
using $AppName.ViewModels;

namespace $AppName;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(
            runtimeService: new SinglePageRuntimeService("$AppName"),
            closeAction: Close,
            minimizeAction: () => WindowState = WindowState.Minimized);

        DataContext = _viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.TryInitialize(out var runtime))
        {
            return;
        }

        if (MainShell.ShellContent is $workspaceType page)
        {
            page.Initialize(
                runtime.MachineName,
                runtime.MachineId,
                runtime.Ip,
                runtime.Port,
                runtime.PollIntervalMs,
                runtime.AutoConnect,
                runtime.MonitorAddress);
        }

        _viewModel.AttachEvents();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.DetachEvents();
    }

    private void MainShell_OnLogoutRequested(object? sender, EventArgs e) => _viewModel.LogoutCommand.Execute(null);
    private void MainShell_OnMinimizeRequested(object? sender, EventArgs e) => _viewModel.MinimizeCommand.Execute(null);
    private void MainShell_OnCloseRequested(object? sender, EventArgs e) => _viewModel.CloseCommand.Execute(null);
}
"@ | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml.cs") -Encoding UTF8

    $servicesDir = Join-Path $projectDir "Services"
    New-Item -ItemType Directory -Path $servicesDir -Force | Out-Null
    @"
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace $AppName.Services;

internal sealed class SinglePageRuntimeService
{
    private readonly string _projectFolderName;

    public SinglePageRuntimeService(string projectFolderName)
    {
        _projectFolderName = projectFolderName;
    }

    public bool TryLoad(out SinglePageRuntimeConfig runtime)
    {
        runtime = default;

        var configPath = ResolveConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("machine", out var machine) || !root.TryGetProperty("plc", out var plc))
        {
            return false;
        }

        var machineName = machine.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? "Machine" : "Machine";
        var machineId = machine.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? "M1" : "M1";
        var ip = plc.TryGetProperty("ip", out var ipElement) ? ipElement.GetString() ?? "127.0.0.1" : "127.0.0.1";
        var port = plc.TryGetProperty("port", out var portElement) && portElement.TryGetInt32(out var portValue) ? portValue : 5000;
        var interval = plc.TryGetProperty("pollIntervalMs", out var intervalElement) && intervalElement.TryGetInt32(out var intervalValue) ? intervalValue : 150;
        var autoConnect = !plc.TryGetProperty("autoConnect", out var autoElement) || autoElement.GetBoolean();

        var monitorAddress = BuildMonitorAddress(root);
        if (plc.TryGetProperty("manualMonitorAddress", out var manualMonitor) && manualMonitor.ValueKind == JsonValueKind.String)
        {
            var manual = manualMonitor.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(manual))
            {
                monitorAddress = string.IsNullOrWhiteSpace(monitorAddress)
                    ? manual
                    : $"{monitorAddress},{manual}";
            }
        }

        runtime = new SinglePageRuntimeConfig(machineName, machineId, ip, port, interval, autoConnect, monitorAddress);
        return true;
    }

    private string ResolveConfigPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; depth < 10 && current != null; depth++)
        {
            var projectConfig = Path.Combine(current.FullName, _projectFolderName, "Config", "Machine1.config.json");
            if (File.Exists(projectConfig))
            {
                return projectConfig;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "Config", "Machine1.config.json");
    }

    private static string BuildMonitorAddress(JsonElement root)
    {
        if (!root.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var expanded = new List<(string Prefix, int Number, string Raw)>();
        AddSectionAddresses(tags, "status", expanded);
        AddSectionAddresses(tags, "process", expanded);

        if (expanded.Count == 0)
        {
            return string.Empty;
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
                result.Add((baseAddress.Value.Prefix, baseAddress.Value.Number + i, $"{baseAddress.Value.Prefix}{baseAddress.Value.Number + i}"));
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

        return int.TryParse(match.Groups[2].Value, out var number)
            ? (match.Groups[1].Value.ToUpperInvariant(), number)
            : null;
    }
}

internal readonly record struct SinglePageRuntimeConfig(
    string MachineName,
    string MachineId,
    string Ip,
    int Port,
    int PollIntervalMs,
    bool AutoConnect,
    string MonitorAddress);
"@ | Set-Content -Path (Join-Path $servicesDir "SinglePageRuntimeService.cs") -Encoding UTF8

    $viewModelsDir = Join-Path $projectDir "ViewModels"
    New-Item -ItemType Directory -Path $viewModelsDir -Force | Out-Null
    @"
using $AppName.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace $AppName.ViewModels;

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly SinglePageRuntimeService _runtimeService;
    private string _headerDeviceName = "SINGLE-DESIGNER";
    private string _pageTitle = "Single Detail Designer";

    public MainWindowViewModel(SinglePageRuntimeService runtimeService, Action closeAction, Action minimizeAction)
    {
        _runtimeService = runtimeService;
        CloseCommand = new RelayCommand(_ => closeAction());
        LogoutCommand = new RelayCommand(_ => closeAction());
        MinimizeCommand = new RelayCommand(_ => minimizeAction());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CloseCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand MinimizeCommand { get; }

    public string HeaderDeviceName
    {
        get => _headerDeviceName;
        private set
        {
            if (_headerDeviceName == value) return;
            _headerDeviceName = value;
            OnPropertyChanged();
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set
        {
            if (_pageTitle == value) return;
            _pageTitle = value;
            OnPropertyChanged();
        }
    }

    public bool TryInitialize(out SinglePageRuntimeConfig runtime)
    {
        if (!_runtimeService.TryLoad(out runtime))
        {
            return false;
        }

        HeaderDeviceName = runtime.MachineName;
        PageTitle = $"{runtime.MachineName} - Single Detail";
        return true;
    }

    public void AttachEvents()
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
        PlcEventContext.EventTriggered += OnPlcEventTriggered;
    }

    public void DetachEvents()
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
    }

    private void OnPlcEventTriggered(object? sender, PlcEventTriggeredEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.EventName))
        {
            return;
        }

        HandlePlcEvent(e.EventName, e);
    }

    private void HandlePlcEvent(string eventName, PlcEventTriggeredEventArgs e)
    {
        switch (eventName.Trim().ToLowerInvariant())
        {
            case "recipestart":
                ShowEventMessage("RecipeStart", e.Address);
                break;
            default:
                ShowEventMessage(eventName, e.Address);
                break;
        }
    }

    private void ShowEventMessage(string eventName, string address)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        PageTitle = $"{HeaderDeviceName} - {eventName} @ {timestamp}";
        CyberMessageBox.Show(
            message: $"PLC Event Triggered\nName: {eventName}\nAddress: {address}\nTime: {timestamp}",
            title: "PLC Event",
            buttons: MessageBoxButton.OK,
            icon: MessageBoxImage.Information);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
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
"@ | Set-Content -Path (Join-Path $viewModelsDir "MainWindowViewModel.cs") -Encoding UTF8
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
    "autoConnect": false,
    "manualMonitorAddress": ""
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
4. Optional layout preset at generation: `-DesignerLayoutPreset ThreeColumn|TwoColumn64|TwoByTwo`
5. For `TwoColumn64`, adjust ratio with: `-DesignerSplitLeftWeight <N> -DesignerSplitRightWeight <N>`

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
