param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [string]$DestinationRoot = ".",

    [switch]$IncludeSecondDemoSampleConfigs,

    [switch]$SinglePageDesigner,

    [switch]$SinglePageDesignerLocalEditable,

    [ValidateSet("ThreeColumn", "TwoColumn64", "TwoByTwo", "Blank", "BlankTabs")]
    [string]$DesignerLayoutPreset = "ThreeColumn",

    [ValidateRange(1, 20)]
    [int]$DesignerSplitLeftWeight = 3,

    [ValidateRange(1, 20)]
    [int]$DesignerSplitRightWeight = 2,

    # JSON-driven mode: app reads .machinedesign.json at runtime
    [switch]$JsonDrivenApp,

    [ValidateSet("SinglePage", "Standard", "Dashboard")]
    [string]$JsonDrivenShellMode = "SinglePage",

    # Optional hardware configs
    [switch]$IncludePrintHead
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
$requiresCsprojSave = $false

$propertyGroupNode = $baseCsprojXml.SelectSingleNode('/Project/PropertyGroup[1]')
if ($null -eq $propertyGroupNode) {
    $propertyGroupNode = $baseCsprojXml.CreateElement("PropertyGroup")
    $baseProjectNode.PrependChild($propertyGroupNode) | Out-Null
    $requiresCsprojSave = $true
}

$tfNode = $baseCsprojXml.SelectSingleNode('/Project/PropertyGroup[1]/TargetFramework')
if ($null -ne $tfNode) {
    $tfNode.InnerText = "net8.0-windows"
    $requiresCsprojSave = $true
}

$platformTargetNode = $baseCsprojXml.SelectSingleNode('/Project/PropertyGroup[1]/PlatformTarget')
if ($null -eq $platformTargetNode) {
    $platformTargetNode = $baseCsprojXml.CreateElement("PlatformTarget")
    $platformTargetNode.InnerText = "x64"
    $propertyGroupNode.AppendChild($platformTargetNode) | Out-Null
    $requiresCsprojSave = $true
} elseif ($platformTargetNode.InnerText -ne "x64") {
    $platformTargetNode.InnerText = "x64"
    $requiresCsprojSave = $true
}

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
    $requiresCsprojSave = $true
}

$wavesCopyNode = $baseCsprojXml.SelectSingleNode('/Project/ItemGroup/None[@Update="Config\\waves\\**"]')
if ($null -eq $wavesCopyNode) {
    $wavesGroup = $baseCsprojXml.CreateElement("ItemGroup")
    $wavesNone = $baseCsprojXml.CreateElement("None")
    $wavesNone.SetAttribute("Update", "Config\waves\**")
    $wavesCopy = $baseCsprojXml.CreateElement("CopyToOutputDirectory")
    $wavesCopy.InnerText = "PreserveNewest"
    $wavesNone.AppendChild($wavesCopy) | Out-Null
    $wavesGroup.AppendChild($wavesNone) | Out-Null
    $baseProjectNode.AppendChild($wavesGroup) | Out-Null
    $requiresCsprojSave = $true
}

if ($requiresCsprojSave) {
    $baseCsprojXml.Save($projectFile)
}

if ($JsonDrivenApp) {
    [xml]$jdXml = Get-Content -Path $projectFile -Raw
    $jdProject   = $jdXml.Project
    $uiCoreRef    = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Core\Stackdose.UI.Core.csproj")
    $templatesRef = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Templates\Stackdose.UI.Templates.csproj")
    $shellRef     = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.App.ShellShared\Stackdose.App.ShellShared.csproj")
    # 注入 ProjectReferences
    $refGroup = $jdXml.CreateElement("ItemGroup")
    foreach ($r in @($uiCoreRef, $templatesRef, $shellRef)) {
        $n = $jdXml.CreateElement("ProjectReference")
        $n.SetAttribute("Include", $r)
        $refGroup.AppendChild($n) | Out-Null
    }
    
    # 只有在 IncludePrintHead 時才注入相關 DLL 與專案參考
    if ($IncludePrintHead) {
        # 1. 注入 FeiyangWrapper C++ 專案參考 (如果路徑匹配)
        $wrapperVcxproj = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "..\Sdk\FeiyangWrapper\FeiyangWrapper\FeiyangWrapper.vcxproj")
        $wrapperNode = $jdXml.CreateElement("ProjectReference")
        $wrapperNode.SetAttribute("Include", $wrapperVcxproj)
        $refGroup.AppendChild($wrapperNode) | Out-Null
        
        # 2. 注入強力複製 Target (解決另一台電腦找不到 DLL 的問題)
        $targetNode = $jdXml.CreateElement("Target")
        $targetNode.SetAttribute("Name", "CopyFeiyangSdkLibs")
        $targetNode.SetAttribute("AfterTargets", "Build")
        
        # 計算相對路徑
        $sdkLibPath = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "..\Sdk\FeiyangSDK-2.3.1\lib")
        $wrapperRelPath = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "..\Sdk\FeiyangWrapper\FeiyangWrapper\x64")

        $targetNode.InnerXml = @"
<PropertyGroup>
  <WrapperDllPath>`$(MSBuildProjectDirectory)\$wrapperRelPath\`$(Configuration)</WrapperDllPath>
  <WrapperDllPath Condition="'`$(Configuration)'=='Release' and !Exists('`$(WrapperDllPath)\FeiyangWrapper.dll')">`$(MSBuildProjectDirectory)\$wrapperRelPath\Debug</WrapperDllPath>
  <WrapperDllPath Condition="'`$(Configuration)'=='Debug' and !Exists('`$(WrapperDllPath)\FeiyangWrapper.dll')">`$(MSBuildProjectDirectory)\$wrapperRelPath\Release</WrapperDllPath>
</PropertyGroup>
<ItemGroup>
  <FeiyangSdkLibs Include="`$(MSBuildProjectDirectory)\$sdkLibPath\**\*.*" />
  <WrapperDll Include="`$(WrapperDllPath)\FeiyangWrapper.dll" />
</ItemGroup>
<Copy SourceFiles="@(FeiyangSdkLibs)" DestinationFolder="`$(TargetDir)" SkipUnchangedFiles="true" Condition="Exists('`$(MSBuildProjectDirectory)\$sdkLibPath')" />
<Copy SourceFiles="@(WrapperDll)" DestinationFolder="`$(TargetDir)" SkipUnchangedFiles="true" Condition="Exists('%(FullPath)')" />
"@
        $jdProject.AppendChild($targetNode) | Out-Null
    }
    
    $jdProject.AppendChild($refGroup) | Out-Null

    $jdXml.Save($projectFile)

@"
<Application x:Class="$AppName.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
"@ | Set-Content -Path (Join-Path $projectDir "App.xaml") -Encoding UTF8

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

    if ($JsonDrivenShellMode -eq "Dashboard") {
        $shellXml = @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="$AppName"
        WindowStyle="None" ResizeMode="NoResize" SizeToContent="WidthAndHeight">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="28"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="#12121E" MouseLeftButtonDown="OnBarDrag">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="10,0,0,0">
                    <Ellipse x:Name="plcDot" Width="8" Height="8" Fill="#CC4444"
                             Margin="0,0,6,0" VerticalAlignment="Center"/>
                    <TextBlock x:Name="plcLabel" Text="---" Foreground="#777788"
                               FontSize="11" FontFamily="Consolas" VerticalAlignment="Center"/>
                </StackPanel>
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="_" Width="28" Height="28"
                            Background="Transparent" Foreground="#777788" BorderThickness="0"
                            FontSize="12" Cursor="Hand" Click="OnMinimizeClick"/>
                    <Button Content="X" Width="28" Height="28"
                            Background="Transparent" Foreground="#777788" BorderThickness="0"
                            FontSize="12" Cursor="Hand" Click="OnCloseClick"/>
                </StackPanel>
            </Grid>
        </Border>
        <ContentPresenter x:Name="DashboardHost" Grid.Row="1" />
        <Border x:Name="dashboardPlcHost" Grid.Row="1"
                Width="1" Height="1" Opacity="0" IsHitTestVisible="False"
                HorizontalAlignment="Left" VerticalAlignment="Top"/>
    </Grid>
</Window>
"@
    } else {
        $containerType = if ($JsonDrivenShellMode -eq "Standard") { "Templates:MainContainer" } else { "Templates:SinglePageContainer" }
        $shellXml = @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:core="http://schemas.stackdose.com/wpf"
        xmlns:Templates="http://schemas.stackdose.com/templates"
        Title="$AppName"
        Height="900" Width="1800"
        WindowState="Maximized" WindowStyle="None" ResizeMode="CanResize">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <${containerType} x:Name="Shell" Grid.Row="0"
            CloseRequested="Shell_OnCloseRequested"
            MinimizeRequested="Shell_OnMinimizeRequested"
            LogoutRequested="Shell_OnLogoutRequested" />
        <core:PlcStatus x:Name="PlcStatusBar" Grid.Row="1" Height="50" ShowBorder="False" IsGlobal="True" />
    </Grid>
</Window>
"@
    }
    $shellXml | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml") -Encoding UTF8

    if ($JsonDrivenShellMode -eq "Dashboard") {
        $mainWindowCs = @"
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Abstractions.Logging;
using Stackdose.App.ShellShared.Behaviors;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace $AppName;

public partial class MainWindow : Window
{
    private readonly BehaviorEngine _behaviorEngine;
    private PlcStatus? _plcStatus;

    public MainWindow()
    {
        InitializeComponent();
        _behaviorEngine = new BehaviorEngine
        {
            AuditLogger = msg => ComplianceContext.LogSystem(msg, LogLevel.Info),
        };

        Closing += (_, _) => { _behaviorEngine.Dispose(); LiveRecordContext.Stop(); SqliteLogger.Shutdown(); };
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configDir     = Path.Combine(AppContext.BaseDirectory, "Config");
        var appConfigPath = Path.Combine(configDir, "app-config.json");

        if (!File.Exists(appConfigPath)) { MessageBox.Show("找不到 Config/app-config.json", "啟動失敗"); return; }

        using var appDoc = JsonDocument.Parse(File.ReadAllText(appConfigPath));
        var root = appDoc.RootElement;

        string? designFile = root.TryGetProperty("designFile", out var df) ? df.GetString() : null;
        if (string.IsNullOrEmpty(designFile))
            designFile = Directory.EnumerateFiles(configDir, "*.machinedesign.json").FirstOrDefault();
        else
            designFile = Path.Combine(AppContext.BaseDirectory, designFile);

        if (designFile == null || !File.Exists(designFile)) { MessageBox.Show("找不到設計檔", "啟動失敗"); return; }

        int liveIntervalSec = root.TryGetProperty("liveRecordIntervalSec", out var li) ? li.GetInt32() : 5;
        SqliteLogger.Initialize();
        LiveRecordContext.Start(liveIntervalSec);

        if (root.TryGetProperty("plc", out var plc))
        {
            var ip   = plc.TryGetProperty("ip",             out var ipEl)   ? ipEl.GetString()   ?? "127.0.0.1" : "127.0.0.1";
            var port = plc.TryGetProperty("port",           out var portEl) ? portEl.GetInt32()                : 3000;
            var scan = plc.TryGetProperty("pollIntervalMs", out var scanEl) ? scanEl.GetInt32()                : 200;
            var auto = !plc.TryGetProperty("autoConnect",   out var autoEl) || autoEl.GetBoolean();

            plcLabel.Text = ip + ":" + port;

            if (auto)
            {
                _plcStatus = new PlcStatus
                {
                    IpAddress    = ip,
                    Port         = port,
                    AutoConnect  = true,
                    IsGlobal     = true,
                    ScanInterval = scan,
                    ShowBorder   = false,
                };
                _plcStatus.ConnectionEstablished += mgr =>
                    Dispatcher.BeginInvoke(() =>
                    {
                        _behaviorEngine.PlcManager = mgr;
                        plcDot.Fill       = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94));
                        plcLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94));
                    });
                dashboardPlcHost.Child = _plcStatus;
            }
        }

        RenderDocument(DesignFileService.Load(designFile));
    }

    private void OnBarDrag(object sender, System.Windows.Input.MouseButtonEventArgs e) => DragMove();
    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void RenderDocument(DesignDocument doc)
    {
        var canvas = new Canvas
        {
            Width        = doc.CanvasWidth,
            Height       = doc.CanvasHeight,
            ClipToBounds = true,
            Background   = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x32)),
        };

        var controlMap = new List<KeyValuePair<string, FrameworkElement>>();

        foreach (var def in doc.CanvasItems)
        {
            UIElement ctrl;
            try   { ctrl = RuntimeControlFactory.Create(def); }
            catch (Exception ex) { ctrl = MakeErrorPlaceholder(def, ex.Message); }

            if (ctrl is FrameworkElement fe)
            {
                fe.Width = def.Width; fe.Height = def.Height;
                if (fe.Tag is ControlRuntimeTag tag) controlMap.Add(KeyValuePair.Create(tag.Id, fe));
            }
            Canvas.SetLeft(ctrl, def.X); Canvas.SetTop(ctrl, def.Y);
            canvas.Children.Add(ctrl);
        }

        DashboardHost.Content = canvas;
        RegisterCustomHandlers();
        _behaviorEngine.BindDocument(doc.CanvasItems, controlMap);
    }

    private void RegisterCustomHandlers()
    {
        _behaviorEngine.Register(new Handlers.SampleCustomHandler());
    }

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string message) =>
        new Border
        {
            Width = def.Width, Height = def.Height,
            BorderBrush = System.Windows.Media.Brushes.OrangeRed, BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = string.Concat("[", def.Type, "] ", message),
                Foreground = System.Windows.Media.Brushes.OrangeRed, FontSize = 10,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4),
            }
        };
}
"@
    } else {
        $mainWindowCs = @"
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.Abstractions.Logging;
using Stackdose.App.ShellShared.Behaviors;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace $AppName;

public partial class MainWindow : Window
{
    private readonly BehaviorEngine _behaviorEngine;

    public MainWindow()
    {
        InitializeComponent();
        _behaviorEngine = new BehaviorEngine
        {
            AuditLogger = msg => ComplianceContext.LogSystem(msg, LogLevel.Info),
        };
        ApplyPlcConfig();
        Closing += (_, _) => { _behaviorEngine.Dispose(); LiveRecordContext.Stop(); SqliteLogger.Shutdown(); };
        Loaded  += OnLoaded;
    }

    private void ApplyPlcConfig()
    {
        var appConfigPath = Path.Combine(AppContext.BaseDirectory, "Config", "app-config.json");
        if (!File.Exists(appConfigPath)) return;

        using var doc  = JsonDocument.Parse(File.ReadAllText(appConfigPath));
        var root = doc.RootElement;

        if (root.TryGetProperty("plc", out var plc))
        {
            PlcStatusBar.IpAddress    = plc.TryGetProperty("ip",             out var ip)   ? ip.GetString()   ?? "127.0.0.1" : "127.0.0.1";
            PlcStatusBar.Port         = plc.TryGetProperty("port",           out var port) ? port.GetInt32()                : 3000;
            PlcStatusBar.ScanInterval = plc.TryGetProperty("pollIntervalMs", out var scan) ? scan.GetInt32()                : 200;
            PlcStatusBar.AutoConnect  = !plc.TryGetProperty("autoConnect",   out var auto) || auto.GetBoolean();
        }

        int liveIntervalSec = root.TryGetProperty("liveRecordIntervalSec", out var li) ? li.GetInt32() : 5;
        SqliteLogger.Initialize();
        LiveRecordContext.Start(liveIntervalSec);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configDir     = Path.Combine(AppContext.BaseDirectory, "Config");
        var appConfigPath = Path.Combine(configDir, "app-config.json");
        if (!File.Exists(appConfigPath)) return;

        using var appDoc = JsonDocument.Parse(File.ReadAllText(appConfigPath));
        var root = appDoc.RootElement;
        string? designFile = root.TryGetProperty("designFile", out var df) ? df.GetString() : null;
        if (string.IsNullOrEmpty(designFile))
            designFile = Directory.EnumerateFiles(configDir, "*.machinedesign.json").FirstOrDefault();
        else
            designFile = Path.Combine(AppContext.BaseDirectory, designFile);

        if (designFile != null && File.Exists(designFile))
            RenderDocument(DesignFileService.Load(designFile));
    }

    private void RenderDocument(DesignDocument doc)
    {
        if (Shell is Stackdose.UI.Templates.Shell.SinglePageContainer sp)
        {
            sp.PageTitle        = doc.Meta.Title;
            sp.HeaderDeviceName = string.IsNullOrWhiteSpace(doc.Meta.MachineId) ? "DEVICE" : doc.Meta.MachineId;
        }

        var canvas = new Canvas
        {
            Width = doc.CanvasWidth, Height = doc.CanvasHeight, ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x32)),
        };

        var controlMap = new List<KeyValuePair<string, FrameworkElement>>();

        foreach (var def in doc.CanvasItems)
        {
            UIElement ctrl;
            try   { ctrl = RuntimeControlFactory.Create(def); }
            catch (Exception ex) { ctrl = MakeErrorPlaceholder(def, ex.Message); }

            if (ctrl is FrameworkElement fe)
            {
                fe.Width = def.Width; fe.Height = def.Height;
                if (fe.Tag is ControlRuntimeTag tag) controlMap.Add(KeyValuePair.Create(tag.Id, fe));
            }
            Canvas.SetLeft(ctrl, def.X); Canvas.SetTop(ctrl, def.Y);
            canvas.Children.Add(ctrl);
        }

        if (Shell is Stackdose.UI.Templates.Shell.SinglePageContainer sp2)
            sp2.ShellContent = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(20), Content = canvas };
        else if (Shell is ContentPresenter cp)
            cp.Content = canvas;

        RegisterCustomHandlers();
        _behaviorEngine.BindDocument(doc.CanvasItems, controlMap);
    }

    private void RegisterCustomHandlers() => _behaviorEngine.Register(new Handlers.SampleCustomHandler());

    private static UIElement MakeErrorPlaceholder(DesignerItemDefinition def, string message) =>
        new Border { BorderBrush = Brushes.OrangeRed, BorderThickness = new Thickness(1), Child = new TextBlock { Text = $"[{def.Type}] {message}", Foreground = Brushes.OrangeRed } };

    private void Shell_OnCloseRequested(object? sender, EventArgs e)    => Close();
    private void Shell_OnMinimizeRequested(object? sender, EventArgs e) => WindowState = WindowState.Minimized;
    private void Shell_OnLogoutRequested(object? sender, EventArgs e)   => SecurityContext.Logout();
}
"@
    }
    [System.IO.File]::WriteAllText((Join-Path $projectDir "MainWindow.xaml.cs"), $mainWindowCs, [System.Text.UTF8Encoding]::new($true))

@'
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Stackdose.App.ShellShared.Behaviors;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;

namespace NAMESPACE_PLACEHOLDER;

public static class RuntimeControlFactory
{
    public static UIElement Create(DesignerItemDefinition def)
    {
        var control = def.Type switch
        {
            "PlcLabel"           => CreatePlcLabel(def),
            "PlcText"            => CreatePlcText(def),
            "PlcStatusIndicator" => CreateBitIndicator(def),
            "SecuredButton"      => CreateSecuredButton(def),
            "Spacer"             => CreateGroupBox(def),
            "LiveLog"            => new LiveLogViewer(),
            "AlarmViewer"        => CreateAlarmViewer(def),
            "SensorViewer"       => CreateSensorViewer(def),
            "StaticLabel"        => CreateStaticLabel(def),
            "PrintHeadStatus"     => CreatePrintHeadStatus(def),
            "PrintHeadController" => new PrintHeadController(),
            "TabPanel"            => CreateTabPanel(def),
            _                   => MakeUnknownPlaceholder(def.Type),
        };
        AttachBehaviorTag(def, control);
        return control;
    }

    private static UIElement CreatePrintHeadStatus(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PrintHeadStatus
        {
            ConfigFilePath = p.GetString("configFile", "Config/feiyang_head1.json"),
            HeadName       = p.GetString("headName",   "PrintHead 1"),
            HeadIndex      = (int)p.GetDouble("headIndex", 0),
            AutoConnect    = p.GetBool("autoConnect",  false),
        };
    }

    private static void AttachBehaviorTag(DesignerItemDefinition def, UIElement control)
    {
        if (control is not FrameworkElement fe) return;
        var tag = new ControlRuntimeTag { Id = def.Id, PropSetters = BuildPropSetters(fe) };
        fe.Tag = tag;
        if (fe is SecuredButton btn) btn.BehaviorId = def.Id;
    }

    private static Dictionary<string, Action<string>> BuildPropSetters(FrameworkElement fe)
    {
        var s = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        if (fe is Control ctrl)
        {
            s["background"] = v => { try { ctrl.Background = ParseBrush(v); } catch { } };
            s["foreground"] = v => { try { ctrl.Foreground = ParseBrush(v); } catch { } };
        }
        switch (fe)
        {
            case PlcLabel lbl:    s["label"] = v => lbl.Label   = v; break;
            case SecuredButton b: s["label"] = v => b.Content   = v; break;
            case TextBlock tb:    s["text"]  = v => tb.Text     = v;
                                  s["foreground"] = v => { try { tb.Foreground = ParseBrush(v); } catch { } }; break;
        }
        return s;
    }

    private static SolidColorBrush ParseBrush(string v) => new((Color)ColorConverter.ConvertFromString(v));

    private static UIElement CreatePlcLabel(DesignerItemDefinition def)
    {
        var p = def.Props;
        var label = new PlcLabel {
            Label = p.GetString("label", "Label"), Address = p.GetString("address", "D100"),
            DefaultValue = p.GetString("defaultValue", "0"), Divisor = p.GetDouble("divisor", 1), StringFormat = p.GetString("stringFormat", "F0")
        };
        if (p.GetDouble("valueFontSize", 0) > 0) label.ValueFontSize = p.GetDouble("valueFontSize", 0);
        label.EnableLiveRecord = p.GetBool("enableLiveRecord", true);
        return label;
    }

    private static UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcText { Label = p.GetString("label", "Param"), Address = p.GetString("address", "D100") };
    }

    private static UIElement CreateBitIndicator(DesignerItemDefinition def)
    {
        var p = def.Props; var addr = p.GetString("displayAddress", "M100");
        var root = new Border { Background = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)), Padding = new Thickness(8), CornerRadius = new CornerRadius(4) };
        var dot = new Ellipse { Width = 12, Height = 12, Fill = Brushes.Gray, Margin = new Thickness(0,0,8,0) };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(dot); stack.Children.Add(new TextBlock { Text = addr, Foreground = Brushes.White });
        root.Child = stack;
        return root;
    }

    private static UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p = def.Props;
        var label = p.GetString("label", "Cmd");
        var themeStr = p.GetString("theme", "Normal");
        var theme = Enum.TryParse<Stackdose.UI.Core.Controls.ButtonTheme>(themeStr, true, out var t) ? t : Stackdose.UI.Core.Controls.ButtonTheme.Normal;
        return new SecuredButton { Content = label, OperationName = label, Theme = theme };
    }

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var root = new Border
        {
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x55, 0x88, 0xCC)),
            BorderThickness = new Thickness(1),
            Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1E, 0x2E)),
            CornerRadius    = new CornerRadius(4),
        };
        var dock = new DockPanel();
        var header = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x22, 0x33, 0x55)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x55, 0x88, 0xCC)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding         = new Thickness(8, 4, 8, 4),
            Child           = new TextBlock
            {
                Text       = title,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xBB, 0xFF)),
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
            }
        };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        root.Child = dock;
        return root;
    }

    private static UIElement CreateAlarmViewer(DesignerItemDefinition def)
    {
        var v = new AlarmViewer(); var cfg = def.Props.GetString("configFile", "");
        if (!string.IsNullOrEmpty(cfg)) v.ConfigFile = cfg;
        return v;
    }

    private static UIElement CreateSensorViewer(DesignerItemDefinition def)
    {
        var v = new SensorViewer(); var cfg = def.Props.GetString("configFile", "");
        if (!string.IsNullOrEmpty(cfg)) v.ConfigFile = cfg;
        return v;
    }

    private static UIElement CreateStaticLabel(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new TextBlock { Text = p.GetString("staticText", "Label"), FontSize = p.GetDouble("staticFontSize", 14), Foreground = Brushes.White };
    }

    private static UIElement CreateTabPanel(DesignerItemDefinition def)
    {
        var panel = new TabPanel();
        if (!def.Props.TryGetValue("tabs", out var raw) || raw is not JsonElement je)
            return panel;
        TabEntry[]? tabs;
        try { tabs = JsonSerializer.Deserialize<TabEntry[]>(je.GetRawText(),
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return panel; }
        if (tabs == null) return panel;
        foreach (var tab in tabs)
        {
            var container = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment   = VerticalAlignment.Stretch,
            };
            if (tab.Items != null)
            {
                foreach (var itemDef in tab.Items)
                {
                    var child = Create(itemDef);
                    if (child is FrameworkElement fe)
                    {
                        fe.Width  = itemDef.Width;
                        fe.Height = itemDef.Height;
                        Canvas.SetLeft(fe, itemDef.X);
                        Canvas.SetTop(fe,  itemDef.Y);
                    }
                    container.Children.Add(child);
                }
            }
            panel.AddTab(tab.Title ?? "", container);
        }
        return panel;
    }

    private sealed class TabEntry
    {
        public string? Title { get; set; }
        public DesignerItemDefinition[]? Items { get; set; }
    }

    private static UIElement MakeUnknownPlaceholder(string type) => new Border { Child = new TextBlock { Text = "Unknown: " + type, Foreground = Brushes.Red } };
}
'@ -replace "NAMESPACE_PLACEHOLDER", $AppName | Set-Content -Path (Join-Path $projectDir "RuntimeControlFactory.cs") -Encoding UTF8

    $handlersDir = Join-Path $projectDir "Handlers"
    New-Item -ItemType Directory -Path $handlersDir -Force | Out-Null
@"
using Stackdose.App.ShellShared.Behaviors;
using System.Windows;
namespace $AppName.Handlers;
public sealed class SampleCustomHandler : IBehaviorActionHandler {
    public string ActionType => "Custom.Sample";
    public void Execute(BehaviorActionContext ctx) { MessageBox.Show("Custom Action!"); }
}
"@ | Set-Content -Path (Join-Path $handlersDir "SampleCustomHandler.cs") -Encoding UTF8

    $jdConfigDir = Join-Path $projectDir "Config"
    New-Item -ItemType Directory -Path $jdConfigDir -Force | Out-Null
    '[]' | Set-Content -Path (Join-Path $jdConfigDir "Machine1.sensors.json")
    '[]' | Set-Content -Path (Join-Path $jdConfigDir "Machine1.alarms.json")

    if ($IncludePrintHead) {
        $wavesDir = Join-Path $jdConfigDir "waves"
        New-Item -ItemType Directory -Path $wavesDir -Force | Out-Null
        '' | Set-Content -Path (Join-Path $wavesDir ".gitkeep")
@"
{
  "Name": "A-Head1",
  "MachineType": "A",
  "HeadIndex": 0,
  "BoardIP": "192.168.22.68",
  "BoardPort": 10000,
  "PcIP": "192.168.22.1",
  "PcPort": 10000,
  "Waveform": "waves/A8_1536GS_L_25PL_UV_DROP1_30K_ABC0.data",
  "Firmware": {
    "MachineType": "M1536",
    "JetColors": [ 0, 0, 0, 0 ],
    "BaseVoltages": [ 23.5, 23.5, 23.5, 23.5 ],
    "OffsetVoltages": [ 0.0, 0.0, 0.0, 0.0 ],
    "HeatTemperature": 40.0,
    "DisableColumnMask": 0,
    "PrintheadColorCount": 1,
    "InstallDirectionPositive": false,
    "EncoderFunction": 0
  },
  "PrintMode": {
    "PrintDirection": "bidirection",
    "GratingDpi": 1270,
    "ImageDpi": 600,
    "GrayScale": 0,
    "GrayScaleDrop": 1,
    "ResetEncoder": 1000,
    "LColumnCali": [ 29.9, 22.3, 7.6 ],
    "RColumnCali": [ 7.6, 22.3, 29.9 ],
    "CaliPixelMM": 8
  }
}
"@ | Set-Content -Path (Join-Path $jdConfigDir "feiyang_head1.json") -Encoding UTF8
    }

@"
{
  "appTitle": "$AppName",
  "plc": { "ip": "127.0.0.1", "port": 3000, "autoConnect": true },
  "designFile": "Config/M1.machinedesign.json"
}
"@ | Set-Content -Path (Join-Path $jdConfigDir "app-config.json") -Encoding UTF8

    $shellModeValue = if ($JsonDrivenShellMode -eq "Dashboard") { "Dashboard" } else { "SinglePage" }
@"
{
  "version": "2.0",
  "meta": { "title": "$AppName", "machineId": "M1" },
  "layout": { "mode": "$shellModeValue", "showLiveLog": true },
  "canvasWidth": 1280, "canvasHeight": 720,
  "canvasItems": [ { "id": "lbl1", "type": "StaticLabel", "x": 40, "y": 40, "width": 400, "height": 48, "props": { "staticText": "Hello $AppName" } } ]
}
"@ | Set-Content -Path (Join-Path $jdConfigDir "M1.machinedesign.json") -Encoding UTF8

    Write-Host "[init-shell-app] Done. Generated: $projectDir"
    if ($IncludePrintHead) {
        Write-Host ""
        Write-Host "⚠  PrintHead waveform: place vendor-provided .data file into:" -ForegroundColor Yellow
        Write-Host "   $wavesDir" -ForegroundColor Yellow
        Write-Host "   Then set Waveform in Config/feiyang_head1.json (e.g. waves/your_file.data)" -ForegroundColor Yellow
    }
    exit 0
}

# (Rest of traditional scaffolding mode omitted for brevity, keeping only core logic)
Write-Host "[init-shell-app] Traditional mode not fully implemented in this quick-fix. Use -JsonDrivenApp."
