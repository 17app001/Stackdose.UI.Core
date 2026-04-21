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

    [ValidateSet("SinglePage", "Standard")]
    [string]$JsonDrivenShellMode = "SinglePage"
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

if ($requiresCsprojSave) {
    $baseCsprojXml.Save($projectFile)
}

# ─────────────────────────────────────────────────────────────────────────────
# JSON-Driven App Mode
# 產生一個獨立機型專案，會在啟動時讀 Config/*.machinedesign.json
# 使用 BehaviorEngine 執行 events[]，並可在 Handlers/ 放機型專屬 C# 邏輯
# ─────────────────────────────────────────────────────────────────────────────
if ($JsonDrivenApp) {

    # -- 1. Patch csproj: 鎖 net8.0-windows + 加入四個 ProjectReference -------
    [xml]$jdXml = Get-Content -Path $projectFile -Raw
    $jdProject   = $jdXml.Project

    # 強制鎖 net8.0-windows（dotnet new wpf 可能產生 net10.0-windows）
    $tfNode = $jdXml.SelectSingleNode('/Project/PropertyGroup[1]/TargetFramework')
    if ($null -ne $tfNode -and $tfNode.InnerText -ne "net8.0-windows") {
        $tfNode.InnerText = "net8.0-windows"
        $jdXml.Save($projectFile)
        [xml]$jdXml = Get-Content -Path $projectFile -Raw
        $jdProject   = $jdXml.Project
    }
    $uiCoreRef    = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Core\Stackdose.UI.Core.csproj")
    $templatesRef = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.UI.Templates\Stackdose.UI.Templates.csproj")
    $shellRef     = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.App.ShellShared\Stackdose.App.ShellShared.csproj")
    $designerRef  = Get-RelativePath -From $projectDir -To (Join-Path $repoRoot "Stackdose.Tools.MachinePageDesigner\Stackdose.Tools.MachinePageDesigner.csproj")
    $refGroup = $jdXml.CreateElement("ItemGroup")
    foreach ($r in @($uiCoreRef, $templatesRef, $shellRef, $designerRef)) {
        $n = $jdXml.CreateElement("ProjectReference")
        $n.SetAttribute("Include", $r)
        $refGroup.AppendChild($n) | Out-Null
    }
    $jdProject.AppendChild($refGroup) | Out-Null
    $jdXml.Save($projectFile)

    # -- 2. App.xaml -----------------------------------------------------------
@"
<Application x:Class="$AppName.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources />
</Application>
"@ | Set-Content -Path (Join-Path $projectDir "App.xaml") -Encoding UTF8

    # -- 3. App.xaml.cs --------------------------------------------------------
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

    # -- 4. MainWindow.xaml ----------------------------------------------------
    if ($JsonDrivenShellMode -eq "Standard") {
        $shellXml = @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Templates="http://schemas.stackdose.com/templates"
        Title="$AppName"
        Height="900" Width="1800"
        WindowState="Maximized" WindowStyle="None" ResizeMode="CanResize">
    <!-- Standard 模式：MainContainer 由 MainWindow.xaml.cs 動態建立 -->
    <ContentPresenter x:Name="RootContent" />
</Window>
"@
    } else {
        $shellXml = @"
<Window x:Class="$AppName.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Templates="http://schemas.stackdose.com/templates"
        Title="$AppName"
        Height="900" Width="1800"
        WindowState="Maximized" WindowStyle="None" ResizeMode="CanResize">
    <Templates:SinglePageContainer x:Name="Shell"
        CloseRequested="Shell_OnCloseRequested"
        MinimizeRequested="Shell_OnMinimizeRequested"
        LogoutRequested="Shell_OnLogoutRequested" />
</Window>
"@
    }
    $shellXml | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml") -Encoding UTF8

    # -- 5. MainWindow.xaml.cs -------------------------------------------------
    if ($JsonDrivenShellMode -eq "Standard") {
        $mainWindowCs = @"
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stackdose.App.ShellShared.Behaviors;
using Stackdose.App.ShellShared.Services;
using Stackdose.Tools.MachinePageDesigner.Models;
using Stackdose.Tools.MachinePageDesigner.Services;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Templates.Controls;
using Stackdose.UI.Templates.Shell;

namespace $AppName;

public partial class MainWindow : Window
{
    private readonly BehaviorEngine _behaviorEngine;

    public MainWindow()
    {
        InitializeComponent();
        _behaviorEngine = new BehaviorEngine
        {
            AuditLogger = msg => ComplianceContext.LogSystem(msg, Abstractions.Logging.LogLevel.Info),
        };
        Closing += (_, _) => _behaviorEngine.Dispose();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var jsonFile  = Directory.EnumerateFiles(configDir, "*.machinedesign.json").FirstOrDefault();
        if (jsonFile == null) { MessageBox.Show("找不到 Config/*.machinedesign.json", "啟動失敗"); return; }
        var doc = DesignFileService.Load(jsonFile);
        RenderDocument(doc);
    }

    private void RenderDocument(DesignDocument doc)
    {
        var container = new MainContainer
        {
            PageTitle        = doc.Meta.Title,
            HeaderDeviceName = string.IsNullOrWhiteSpace(doc.Meta.MachineId) ? "DEVICE" : doc.Meta.MachineId,
        };
        container.CloseRequested    += (_, _) => Close();
        container.MinimizeRequested += (_, _) => WindowState = WindowState.Minimized;
        container.LogoutRequested   += (_, _) => SecurityContext.Logout();

        RootContent.Content = container;
        SetupMultiPageNavigation(container, doc);
    }

    private void SetupMultiPageNavigation(MainContainer container, DesignDocument doc)
    {
        var pageViews   = new Dictionary<string, UIElement>();
        var allItems    = new List<IControlWithBehaviors>();
        var allControls = new List<KeyValuePair<string, System.Windows.FrameworkElement>>();

        foreach (var page in doc.Pages)
        {
            var canvas = new Canvas
            {
                Width = doc.CanvasWidth, Height = doc.CanvasHeight, ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x32)),
            };
            foreach (var def in page.CanvasItems)
            {
                UIElement ctrl;
                try   { ctrl = RuntimeControlFactory.Create(def); }
                catch (Exception ex) { ctrl = MakeErrorPlaceholder(def, ex.Message); }
                if (ctrl is System.Windows.FrameworkElement fe)
                {
                    fe.Width = def.Width; fe.Height = def.Height;
                    if (fe.Tag is ControlRuntimeTag tag) allControls.Add(KeyValuePair.Create(tag.Id, fe));
                }
                Canvas.SetLeft(ctrl, def.X); Canvas.SetTop(ctrl, def.Y);
                canvas.Children.Add(ctrl);
            }
            allItems.AddRange(page.CanvasItems);
            pageViews[page.Id] = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                Padding = new Thickness(20), Content = canvas,
            };
        }

        container.NavigationItems = new ObservableCollection<NavigationItem>(
            doc.Pages.Select(p => new NavigationItem { Title = p.Title, NavigationTarget = p.Id }));

        void Navigate(string pageId)
        {
            if (!pageViews.TryGetValue(pageId, out var view)) return;
            var page = doc.Pages.FirstOrDefault(p => p.Id == pageId);
            container.ShellContent = view;
            container.PageTitle    = page?.Title ?? pageId;
            container.SelectNavigationTarget(pageId);
        }

        container.NavigationRequested += (_, pageId) => Navigate(pageId);
        _behaviorEngine.Navigator = Navigate;
        RegisterCustomHandlers();
        _behaviorEngine.BindDocument(allItems, allControls);
        if (doc.Pages.Count > 0) Navigate(doc.Pages[0].Id);
    }

    // 在這裡注入機型專屬 Handler
    private void RegisterCustomHandlers()
    {
        // 範例：_behaviorEngine.Register(new Handlers.ModelSStartCycleHandler());
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
        # SinglePage mode
        $mainWindowCs = @"
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            AuditLogger = msg => ComplianceContext.LogSystem(msg, Abstractions.Logging.LogLevel.Info),
        };
        Closing += (_, _) => _behaviorEngine.Dispose();
        Loaded  += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        var jsonFile  = Directory.EnumerateFiles(configDir, "*.machinedesign.json").FirstOrDefault();
        if (jsonFile == null) { MessageBox.Show("找不到 Config/*.machinedesign.json", "啟動失敗"); return; }
        var doc = DesignFileService.Load(jsonFile);
        RenderDocument(doc);
    }

    private void RenderDocument(DesignDocument doc)
    {
        Shell.PageTitle        = doc.Meta.Title;
        Shell.HeaderDeviceName = string.IsNullOrWhiteSpace(doc.Meta.MachineId) ? "DEVICE" : doc.Meta.MachineId;

        var canvas = new Canvas
        {
            Width = doc.CanvasWidth, Height = doc.CanvasHeight, ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x32)),
        };

        var controlMap = new List<KeyValuePair<string, System.Windows.FrameworkElement>>();

        foreach (var def in doc.CanvasItems)
        {
            UIElement ctrl;
            try   { ctrl = RuntimeControlFactory.Create(def); }
            catch (Exception ex) { ctrl = MakeErrorPlaceholder(def, ex.Message); }

            if (ctrl is System.Windows.FrameworkElement fe)
            {
                fe.Width = def.Width; fe.Height = def.Height;
                if (fe.Tag is ControlRuntimeTag tag) controlMap.Add(KeyValuePair.Create(tag.Id, fe));
            }

            Canvas.SetLeft(ctrl, def.X);
            Canvas.SetTop(ctrl, def.Y);
            canvas.Children.Add(ctrl);
        }

        Shell.ShellContent = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            Padding = new Thickness(20),
            Content = canvas,
        };

        RegisterCustomHandlers();
        _behaviorEngine.BindDocument(doc.CanvasItems, controlMap);
    }

    // 在這裡注入機型專屬 Handler
    private void RegisterCustomHandlers()
    {
        // 範例：_behaviorEngine.Register(new Handlers.ModelSStartCycleHandler());
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

    private void Shell_OnCloseRequested(object? sender, EventArgs e)    => Close();
    private void Shell_OnMinimizeRequested(object? sender, EventArgs e) => WindowState = WindowState.Minimized;
    private void Shell_OnLogoutRequested(object? sender, EventArgs e)   => SecurityContext.Logout();
}
"@
    }
    $mainWindowCs | Set-Content -Path (Join-Path $projectDir "MainWindow.xaml.cs") -Encoding UTF8

    # -- 6. RuntimeControlFactory.cs -------------------------------------------
@'
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
            _                   => MakeUnknownPlaceholder(def.Type),
        };
        AttachBehaviorTag(def, control);
        return control;
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
        if (fe is System.Windows.Controls.Control ctrl)
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

    private static SolidColorBrush ParseBrush(string v)
        => new((Color)ColorConverter.ConvertFromString(v));

    private static UIElement CreatePlcLabel(DesignerItemDefinition def)
    {
        var p = def.Props;
        var label = new PlcLabel
        {
            Label        = p.GetString("label",        "Label"),
            Address      = p.GetString("address",      "D100"),
            DefaultValue = p.GetString("defaultValue", "0"),
            Divisor      = p.GetDouble("divisor",      1),
            StringFormat = p.GetString("stringFormat", "F0"),
            ShowAddress  = false,
        };
        if (p.GetDouble("valueFontSize", 0) is > 0 and var vfs) label.ValueFontSize = vfs;
        if (p.GetDouble("labelFontSize", 0) is > 0 and var lfs) label.LabelFontSize = lfs;
        if (Enum.TryParse<HorizontalAlignment>(p.GetString("valueAlignment", ""), true, out var va)) label.ValueAlignment = va;
        if (Enum.TryParse<HorizontalAlignment>(p.GetString("labelAlignment", ""), true, out var la)) label.LabelAlignment = la;
        if (Enum.TryParse<PlcLabelFrameShape>(p.GetString("frameShape", "Rectangle"), true, out var sh)) label.FrameShape = sh;
        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("valueColorTheme", "NeonBlue"), true, out var vt)) label.ValueForeground = vt;
        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("labelForeground", ""), true, out var lt)) label.LabelForeground = lt;
        if (Enum.TryParse<PlcLabelColorTheme>(p.GetString("frameBackground", ""), true, out var bg)) label.FrameBackground = bg;
        return label;
    }

    private static UIElement CreatePlcText(DesignerItemDefinition def)
    {
        var p = def.Props;
        return new PlcText
        {
            Label              = p.GetString("label",              "Parameter"),
            Address            = p.GetString("address",            "D100"),
            ShowSuccessMessage = p.GetBool  ("showSuccessMessage", true),
            EnableAuditTrail   = p.GetBool  ("enableAuditTrail",   true),
        };
    }

    private static UIElement CreateBitIndicator(DesignerItemDefinition def)
    {
        var p       = def.Props;
        var address = p.GetString("displayAddress", "M100");
        var label   = p.GetString("label",          address);
        var root    = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x31, 0x31, 0x45)),
            BorderBrush     = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x5A)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(8),
        };
        var dot   = new Ellipse { Width = 12, Height = 12, Fill = Brushes.Gray, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
        var text  = new TextBlock { Text = string.Concat(label, "  [", address, "]"), Foreground = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)), VerticalAlignment = VerticalAlignment.Center };
        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(dot); stack.Children.Add(text);
        root.Child = stack;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) =>
        {
            try
            {
                var mgr = PlcContext.GlobalStatus?.CurrentManager;
                if (mgr == null || !mgr.IsConnected) { dot.Fill = Brushes.Gray; return; }
                int? val = address.StartsWith("M", StringComparison.OrdinalIgnoreCase)
                    ? (mgr.ReadBit(address) == true ? 1 : 0) : mgr.ReadWord(address);
                dot.Fill = val is > 0
                    ? new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x94))
                    : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88));
            }
            catch { dot.Fill = Brushes.OrangeRed; }
        };
        root.Loaded   += (_, _) => timer.Start();
        root.Unloaded += (_, _) => timer.Stop();
        return root;
    }

    private static UIElement CreateSecuredButton(DesignerItemDefinition def)
    {
        var p     = def.Props;
        var label = p.GetString("label", "Command");
        var theme = p.GetString("theme", "Primary").ToLowerInvariant() switch
        {
            "danger"  or "red"    or "error"  => ButtonTheme.Error,
            "success" or "green"              => ButtonTheme.Success,
            "warning" or "orange"             => ButtonTheme.Warning,
            "info"    or "cyan"               => ButtonTheme.Info,
            "normal"  or "gray"               => ButtonTheme.Normal,
            _                                 => ButtonTheme.Primary,
        };
        return new SecuredButton { Content = label, Theme = theme, OperationName = label, MinWidth = 80 };
    }

    private static UIElement CreateGroupBox(DesignerItemDefinition def)
    {
        var title = def.Props.GetString("title", "Group");
        var root  = new Grid();
        root.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x8E, 0xEF)),
            BorderThickness = new Thickness(1.5),
            Background = new SolidColorBrush(Color.FromArgb(0x18, 0x6C, 0x8E, 0xEF)),
            CornerRadius = new CornerRadius(4), IsHitTestVisible = false,
        });
        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x3A, 0x56, 0xA8)),
            CornerRadius = new CornerRadius(2, 2, 0, 0), Padding = new Thickness(10, 4, 10, 4),
        };
        header.Child = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Group" : title,
            Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeights.SemiBold,
        };
        var dock = new DockPanel { LastChildFill = true, Background = null };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header); dock.Children.Add(new Border { Background = null });
        root.Children.Add(dock);
        return root;
    }

    private static UIElement CreateAlarmViewer(DesignerItemDefinition def)
    {
        var viewer = new AlarmViewer();
        var cfg = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(cfg)) viewer.ConfigFile = cfg;
        return viewer;
    }

    private static UIElement CreateSensorViewer(DesignerItemDefinition def)
    {
        var viewer = new SensorViewer();
        var cfg = def.Props.GetString("configFile", "");
        if (!string.IsNullOrWhiteSpace(cfg)) viewer.ConfigFile = cfg;
        return viewer;
    }

    private static UIElement CreateStaticLabel(DesignerItemDefinition def)
    {
        var p    = def.Props;
        var text = p.GetString("staticText", p.GetString("text", p.GetString("label", "")));
        SolidColorBrush brush;
        try { brush = ParseBrush(p.GetString("staticForeground", p.GetString("foreground", "#E2E2F0"))); }
        catch { brush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xF0)); }
        var weight = p.GetString("staticFontWeight", "Normal").ToLowerInvariant() switch
        {
            "bold"     => FontWeights.Bold, "semibold" => FontWeights.SemiBold,
            "light"    => FontWeights.Light, _         => FontWeights.Normal,
        };
        var align = p.GetString("staticTextAlign", p.GetString("textAlign", "Left")).ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center, "right" => TextAlignment.Right, _ => TextAlignment.Left,
        };
        return new TextBlock
        {
            Text = text, FontSize = p.GetDouble("staticFontSize", p.GetDouble("fontSize", 13)),
            FontWeight = weight, TextAlignment = align, Foreground = brush,
            FontFamily = new System.Windows.Media.FontFamily("Microsoft JhengHei"),
            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap,
        };
    }

    private static UIElement MakeUnknownPlaceholder(string type) =>
        new Border
        {
            BorderBrush = Brushes.OrangeRed, BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0x55, 0x00)),
            Child = new TextBlock
            {
                Text = string.Concat("Unknown: ", type), Foreground = Brushes.OrangeRed,
                FontSize = 11, Margin = new Thickness(6), TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            }
        };
}
'@ -replace "NAMESPACE_PLACEHOLDER", $AppName |
    Set-Content -Path (Join-Path $projectDir "RuntimeControlFactory.cs") -Encoding UTF8

    # -- 7. Handlers/ ----------------------------------------------------------
    $handlersDir = Join-Path $projectDir "Handlers"
    New-Item -ItemType Directory -Path $handlersDir -Force | Out-Null

@"
using Stackdose.App.ShellShared.Behaviors;

namespace $AppName.Handlers;

/// <summary>
/// 機型專屬 Handler 範例。
/// 在 MainWindow.RegisterCustomHandlers() 呼叫 _behaviorEngine.Register(new SampleCustomHandler()) 啟用。
/// 對應 JSON events[].do[] 中 "type": "Custom.Sample"。
/// </summary>
public sealed class SampleCustomHandler : IBehaviorActionHandler
{
    public string ActionType => "Custom.Sample";

    public void Execute(BehaviorActionContext ctx)
    {
        var msg = ctx.Action.Message ?? "SampleCustomHandler executed";
        System.Windows.MessageBox.Show(msg, "Custom Action");
    }
}
"@ | Set-Content -Path (Join-Path $handlersDir "SampleCustomHandler.cs") -Encoding UTF8

    # -- 8. Config/ ------------------------------------------------------------
    $jdConfigDir = Join-Path $projectDir "Config"
    New-Item -ItemType Directory -Path $jdConfigDir -Force | Out-Null

@"
{
  "machine": { "id": "M1", "name": "$AppName" },
  "plc": { "ip": "192.168.22.39", "port": 3000, "pollIntervalMs": 150, "autoConnect": false }
}
"@ | Set-Content -Path (Join-Path $jdConfigDir "Machine1.config.json") -Encoding UTF8

    $shellModeValue = if ($JsonDrivenShellMode -eq "Standard") { "Standard" } else { "SinglePage" }
@"
{
  "meta": { "title": "$AppName", "machineId": "M1" },
  "shellMode": "$shellModeValue",
  "canvasWidth": 1280,
  "canvasHeight": 720,
  "canvasItems": [
    {
      "id": "lbl-001", "type": "StaticLabel",
      "x": 40, "y": 40, "width": 400, "height": 48,
      "props": { "staticText": "Welcome - $AppName", "staticFontSize": 28, "staticFontWeight": "Bold" },
      "events": []
    }
  ],
  "pages": []
}
"@ | Set-Content -Path (Join-Path $jdConfigDir "$AppName.machinedesign.json") -Encoding UTF8

    # -- 9. README -------------------------------------------------------------
@"
# $AppName — JSON-Driven App ($JsonDrivenShellMode 模式)

## 開始使用

1. 用 **MachinePageDesigner** 設計 UI → 存成 `Config/$AppName.machinedesign.json`
2. 在 `MainWindow.RegisterCustomHandlers()` 注入機型專屬 Handler（參考 `Handlers/SampleCustomHandler.cs`）
3. 修改 `Config/Machine1.config.json` 設定 PLC IP / Port
4. 編譯並執行

## 目錄說明

| 目錄/檔案 | 說明 |
|---|---|
| `Config/*.machinedesign.json` | Designer 輸出，app 啟動時自動讀取 |
| `Config/Machine1.config.json` | PLC 連線設定 |
| `Handlers/` | 機型專屬 IBehaviorActionHandler |
| `RuntimeControlFactory.cs` | JSON 控件類型 → WPF 控件的映射 |
| `MainWindow.xaml.cs` | JSON 讀取 + BehaviorEngine 接線 |

## Shell 模式

目前模式：**$JsonDrivenShellMode**

- `SinglePage`：SinglePageContainer（Header + 單一畫布，無 LeftNav）
- `Standard`：MainContainer（Header + LeftNav 多頁導覽）

重新 scaffold 切換模式：`-JsonDrivenApp -JsonDrivenShellMode Standard`
"@ | Set-Content -Path (Join-Path $projectDir "QUICKSTART.md") -Encoding UTF8

    Write-Host "[init-shell-app] Done (JsonDrivenApp / $JsonDrivenShellMode). Generated: $projectDir"
    exit 0
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

                    <templateControls:GroupBoxBlock Grid.Column="0" Header="Control Group A" BadgeText="$($leftRatioPercent)%" GroupPadding="10">
                        <templateControls:GroupBoxBlock.GroupContent>
                            <core:SecuredButton Width="180"
                                                Height="40"
                                                Content="Secured Test Button"
                                                Theme="Info"
                                                RequiredLevel="Operator"
                                                OperationName="Single Page Secured Test"
                                                Click="OnSecuredSampleButtonClick" />
                        </templateControls:GroupBoxBlock.GroupContent>
                    </templateControls:GroupBoxBlock>
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
                        <templateControls:GroupBoxBlock Grid.Column="0" Header="Top Left" BadgeText="Group A" GroupPadding="10">
                            <templateControls:GroupBoxBlock.GroupContent>
                                <core:SecuredButton Width="180"
                                                    Height="40"
                                                    Content="Secured Test Button"
                                                    Theme="Info"
                                                    RequiredLevel="Operator"
                                                    OperationName="Single Page Secured Test"
                                                    Click="OnSecuredSampleButtonClick" />
                            </templateControls:GroupBoxBlock.GroupContent>
                        </templateControls:GroupBoxBlock>
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
    "Blank" {
@"
                <Grid Grid.Row="1"
                      Background="{DynamicResource Surface.Bg.Panel}">
                    <Canvas
                            x:Name="DesignSurface"
                            ClipToBounds="True"
                            Background="{DynamicResource Surface.Bg.Page}">
                        <core:SecuredButton Canvas.Left="24"
                                            Canvas.Top="20"
                                            Width="180"
                                            Height="40"
                                            Content="Secured Test Button"
                                            Theme="Info"
                                            RequiredLevel="Operator"
                                            OperationName="Single Page Secured Test"
                                            Click="OnSecuredSampleButtonClick" />

                        <core:PlcEventTrigger Address="M237"
                                              EventName="RecipeStart"
                                              TriggerCondition="OnRising"
                                              AutoClear="True"
                                              TargetStatus="{Binding ElementName=TopPlcStatus}" />
                    </Canvas>
                </Grid>
"@
    }
    "BlankTabs" {
@"
                <Grid Grid.Row="1"
                      Background="{DynamicResource Surface.Bg.Panel}">
                    <TabControl Style="{StaticResource Template.TabControl}"
                                Margin="0"
                                HorizontalAlignment="Stretch"
                                VerticalAlignment="Stretch">
                        <TabItem Header="Page 1" Style="{StaticResource Template.TabItem}">
                            <Grid Background="{DynamicResource Surface.Bg.Page}">
                                <core:SecuredButton Width="180"
                                                    Height="40"
                                                    Margin="24,20,0,0"
                                                    HorizontalAlignment="Left"
                                                    VerticalAlignment="Top"
                                                    Content="Secured Test Button"
                                                    Theme="Info"
                                                    RequiredLevel="Operator"
                                                    OperationName="Single Page Secured Test"
                                                    Click="OnSecuredSampleButtonClick" />

                                <core:PlcEventTrigger Address="M237"
                                                      EventName="RecipeStart"
                                                      TriggerCondition="OnRising"
                                                      AutoClear="True"
                                                      TargetStatus="{Binding ElementName=TopPlcStatus}" />
                            </Grid>
                        </TabItem>

                        <TabItem Header="Page 2" Style="{StaticResource Template.TabItem}">
                            <Grid Background="{DynamicResource Surface.Bg.Page}" />
                        </TabItem>
                    </TabControl>
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

                    <templateControls:GroupBoxBlock Grid.Column="0" Header="Control Group A" BadgeText="Primary" GroupPadding="10">
                        <templateControls:GroupBoxBlock.GroupContent>
                            <core:SecuredButton Width="180"
                                                Height="40"
                                                Content="Secured Test Button"
                                                Theme="Info"
                                                RequiredLevel="Operator"
                                                OperationName="Single Page Secured Test"
                                                Click="OnSecuredSampleButtonClick" />
                        </templateControls:GroupBoxBlock.GroupContent>
                    </templateControls:GroupBoxBlock>
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
        Width="1800"
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
             d:DesignWidth="1800">

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
using Stackdose.UI.Core.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace $AppName.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public event EventHandler? SecuredSampleButtonClicked;

    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(
        string machineName,
        string machineId,
        string plcIp,
        int plcPort,
        int scanIntervalMs,
        bool autoConnect,
        string monitorAddress,
        string sensorConfigPath,
        string alarmConfigPath)
    {
        MachineSummaryText.Text = $"Machine: {machineName} ({machineId})";
        TopPlcStatus.IpAddress = plcIp;
        TopPlcStatus.Port = plcPort;
        TopPlcStatus.ScanInterval = scanIntervalMs;
        TopPlcStatus.AutoConnect = autoConnect;
        TopPlcStatus.MonitorAddress = monitorAddress;

        BindViewerConfigs(sensorConfigPath, alarmConfigPath);
    }

    private void BindViewerConfigs(string sensorConfigPath, string alarmConfigPath)
    {
        foreach (var viewer in FindVisualChildren<SensorViewer>(this))
        {
            if (string.IsNullOrWhiteSpace(viewer.ConfigFile))
            {
                viewer.ConfigFile = sensorConfigPath;
            }
        }

        foreach (var viewer in FindVisualChildren<AlarmViewer>(this))
        {
            if (string.IsNullOrWhiteSpace(viewer.ConfigFile))
            {
                viewer.ConfigFile = alarmConfigPath;
            }

            if (viewer.TargetStatus == null)
            {
                viewer.TargetStatus = TopPlcStatus;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
        {
            yield break;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private void OnSecuredSampleButtonClick(object sender, RoutedEventArgs e)
    {
        SecuredSampleButtonClicked?.Invoke(this, EventArgs.Empty);
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
        Width="1800"
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
                runtime.MonitorAddress,
                runtime.SensorConfigPath,
                runtime.AlarmConfigPath);

            page.SecuredSampleButtonClicked -= OnSecuredSampleButtonClicked;
            page.SecuredSampleButtonClicked += OnSecuredSampleButtonClicked;
        }

        _viewModel.AttachEvents();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (MainShell.ShellContent is $workspaceType page)
        {
            page.SecuredSampleButtonClicked -= OnSecuredSampleButtonClicked;
        }

        _viewModel.DetachEvents();
    }

    private void OnSecuredSampleButtonClicked(object? sender, EventArgs e) => _viewModel.SecuredSampleButtonCommand.Execute(null);

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
        var sensorConfigFile = root.TryGetProperty("sensorConfigFile", out var sensorElement) && sensorElement.ValueKind == JsonValueKind.String
            ? sensorElement.GetString() ?? "Config/Machine1.sensors.json"
            : "Config/Machine1.sensors.json";
        var alarmConfigFile = root.TryGetProperty("alarmConfigFile", out var alarmElement) && alarmElement.ValueKind == JsonValueKind.String
            ? alarmElement.GetString() ?? "Config/Machine1.alarms.json"
            : "Config/Machine1.alarms.json";
        var sensorConfigPath = ResolveCompanionConfigPath(configPath, sensorConfigFile);
        var alarmConfigPath = ResolveCompanionConfigPath(configPath, alarmConfigFile);

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

        runtime = new SinglePageRuntimeConfig(machineName, machineId, ip, port, interval, autoConnect, monitorAddress, sensorConfigPath, alarmConfigPath);
        return true;
    }

    private static string ResolveCompanionConfigPath(string mainConfigPath, string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        var normalized = configuredPath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            return normalized;
        }

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, normalized);
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var configDir = Path.GetDirectoryName(mainConfigPath) ?? AppContext.BaseDirectory;
        if (normalized.StartsWith($"Config{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            var projectRoot = Directory.GetParent(configDir)?.FullName ?? configDir;
            return Path.Combine(projectRoot, normalized);
        }

        return Path.Combine(configDir, normalized);
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
    string MonitorAddress,
    string SensorConfigPath,
    string AlarmConfigPath);
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
        SecuredSampleButtonCommand = new RelayCommand(_ => ShowSecuredSampleMessage());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CloseCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand SecuredSampleButtonCommand { get; }

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

    private void ShowSecuredSampleMessage()
    {
        CyberMessageBox.Show(
            message: "SecuredButton click event triggered via MainWindowViewModel.",
            title: "SecuredButton Sample",
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

    $singlePageReadme = @(
        "# Shell Quickstart ($singlePageModeTitle)",
        '',
        '1. Edit `Config/Machine1.config.json` for PLC connection and addresses.',
        '2. Open the designer page in Visual Studio:',
        '   - local editable mode: `Pages/SingleDetailWorkspacePage.xaml`',
        '   - template mode: `Templates:SingleDetailWorkspacePage` inside `MainWindow.xaml`',
        '3. Drag `UI.Core` controls into Group A/B/C and run.',
        '4. Optional layout preset at generation: `-DesignerLayoutPreset ThreeColumn|TwoColumn64|TwoByTwo|Blank|BlankTabs`',
        '5. For `TwoColumn64`, adjust ratio with: `-DesignerSplitLeftWeight <N> -DesignerSplitRightWeight <N>`',
        '6. `Blank` preset includes one SecuredButton and one PlcEventTrigger starter.',
        '7. `BlankTabs` preset provides a styled TabControl; add/remove TabItem pages as needed.',
        '',
        'Reference:',
        '- Repo root `Stackdose.App.SingleDetailLab/README_SINGLE_PAGE_QUICKSTART.md`'
    )

    ($singlePageReadme -join "`r`n") | Set-Content -Path $readmePath -Encoding UTF8
} else {
    $shellReadme = @(
        '# Shell Quickstart',
        '',
        '1. Configure your app in `Config/app-meta.json`.',
        '2. Update machine/alarm/sensor json files under `Config/`.',
        '   - required keys in machine config: alarmConfigFile, sensorConfigFile',
        '3. Build and run your project.',
        '',
        'Reference:',
        '- Repo root `QUICKSTART.md` (recommended)',
        '- `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md` (advanced wiring details)'
    )

    ($shellReadme -join "`r`n") | Set-Content -Path $readmePath -Encoding UTF8
}

Write-Host "[init-shell-app] Done. Generated: $projectDir"
