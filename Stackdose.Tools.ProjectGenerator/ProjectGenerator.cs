using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Stackdose.Tools.ProjectGenerator;

/// <summary>
/// Generates a complete DeviceFramework WPF project from a parsed DeviceSpec.
/// All generated files compile immediately with dotnet build.
/// </summary>
public sealed class ProjectGenerator
{
    private readonly DeviceSpec _spec;
    private readonly string _outputRoot;
    private readonly List<string> _generatedFiles = [];

    public ProjectGenerator(DeviceSpec spec, string outputRoot)
    {
        _spec = spec;
        _outputRoot = Path.Combine(outputRoot, spec.Project.ProjectName);
    }

    public IReadOnlyList<string> GeneratedFiles => _generatedFiles;

    private bool HasPanel(string panelType) =>
        _spec.Panels.Any(p => p.PanelType.Equals(panelType, StringComparison.OrdinalIgnoreCase));

    private bool IsSinglePage =>
        _spec.Project.PageMode.Equals("SinglePage", StringComparison.OrdinalIgnoreCase);

    public void Generate()
    {
        Directory.CreateDirectory(_outputRoot);
        Directory.CreateDirectory(Path.Combine(_outputRoot, "Config"));

        GenerateCsproj();
        GenerateAppXaml();
        GenerateAppXamlCs();
        GenerateMainWindowXaml();
        GenerateMainWindowXamlCs();
        GenerateAppMeta();

        foreach (var machine in _spec.Machines)
            GenerateMachineConfig(machine);

        if (IsSinglePage)
            return;

        GenerateCommandHandlers();
        GenerateDataEventHandlers();

        if (_spec.Project.PageMode.Equals("CustomPage", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.Combine(_outputRoot, "Pages"));
            GenerateCustomPageXaml();
            GenerateCustomPageXamlCs();
        }

        if (HasPanel("MaintenanceMode"))
        {
            Directory.CreateDirectory(Path.Combine(_outputRoot, "Pages"));
            GenerateMaintenancePageXaml();
            GenerateMaintenancePageXamlCs();
            GenerateMaintenanceHandlers();
        }

        if (HasPanel("Settings"))
        {
            Directory.CreateDirectory(Path.Combine(_outputRoot, "Pages"));
            GenerateSettingsPageXaml();
            GenerateSettingsPageXamlCs();
        }
    }

    // ═══════════════════════════════════════
    //  .csproj
    // ═══════════════════════════════════════

    private void GenerateCsproj()
    {
        var content = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net8.0-windows</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <UseWPF>true</UseWPF>
                <PlatformTarget>x64</PlatformTarget>
                <RootNamespace>{_spec.Project.ProjectName}</RootNamespace>
                <AssemblyName>{_spec.Project.ProjectName}</AssemblyName>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\Stackdose.App.DeviceFramework\Stackdose.App.DeviceFramework.csproj" />
                <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
                <ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
              </ItemGroup>

              <ItemGroup>
                <None Update="Config\**\*.json">
                  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
                </None>
              </ItemGroup>

            </Project>
            """;
        WriteFile($"{_spec.Project.ProjectName}.csproj", content);
    }

    // ═══════════════════════════════════════
    //  App.xaml
    // ═══════════════════════════════════════

    private void GenerateAppXaml()
    {
        var ns = _spec.Project.ProjectName;
        var content = $"""
            <Application x:Class="{ns}.App"
                         xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Application.Resources>
                    <ResourceDictionary>
                        <ResourceDictionary.MergedDictionaries>
                            <ResourceDictionary Source="pack://application:,,,/Stackdose.UI.Core;component/Themes/Theme.xaml"/>
                            <ResourceDictionary Source="pack://application:,,,/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
                        </ResourceDictionary.MergedDictionaries>
                    </ResourceDictionary>
                </Application.Resources>
            </Application>
            """;
        WriteFile("App.xaml", content);
    }

    // ═══════════════════════════════════════
    //  App.xaml.cs
    // ═══════════════════════════════════════

    private void GenerateAppXamlCs()
    {
        var ns = _spec.Project.ProjectName;

        if (IsSinglePage)
        {
            var singleContent = $$"""
                using Stackdose.UI.Core.Helpers;
                using Stackdose.UI.Core.Models;
                using Stackdose.UI.Templates.Helpers;
                using System.Windows;

                namespace {{ns}};

                public partial class App : Application
                {
                    protected override void OnStartup(StartupEventArgs e)
                    {
                        AppThemeBootstrapper.Apply(this);
                        base.OnStartup(e);

                        // Auto-login as SuperAdmin (no login dialog in SinglePage mode)
                        SecurityContext.CurrentSession.CurrentUser = new UserAccount
                        {
                            UserId = "superadmin",
                            DisplayName = "Super Admin",
                            AccessLevel = AccessLevel.SuperAdmin
                        };

                        var mainWindow = new MainWindow();
                        MainWindow = mainWindow;
                        mainWindow.Show();
                    }
                }
                """;
            WriteFile("App.xaml.cs", singleContent);
            return;
        }

        var content = $$"""
            using Stackdose.UI.Core.Controls;
            using Stackdose.UI.Templates.Helpers;
            using System.Windows;

            namespace {{ns}};

            public partial class App : Application
            {
                protected override void OnStartup(StartupEventArgs e)
                {
                    AppThemeBootstrapper.Apply(this);
                    base.OnStartup(e);

                    ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    bool loginSuccess = LoginDialog.ShowLoginDialog();
                    if (!loginSuccess) { Shutdown(); return; }

                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    var mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
            }
            """;
        WriteFile("App.xaml.cs", content);
    }

    // ═══════════════════════════════════════
    //  MainWindow.xaml
    // ═══════════════════════════════════════

    private void GenerateSinglePageMainWindowXaml()
    {
        var ns = _spec.Project.ProjectName;
        var header = _spec.Project.HeaderDeviceName;
        var content = $$"""
            <Window x:Class="{{ns}}.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:core="clr-namespace:Stackdose.UI.Core.Controls;assembly=Stackdose.UI.Core"
                    xmlns:tmpl="clr-namespace:Stackdose.UI.Templates.Controls;assembly=Stackdose.UI.Templates"
                    Title="{{header}}"
                    Height="900" Width="1600"
                    WindowState="Maximized"
                    WindowStyle="None"
                    ResizeMode="CanResize"
                    Background="{StaticResource BackgroundBrush}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                        <RowDefinition Height="5" />
                        <RowDefinition Height="32" />
                        <RowDefinition Height="260" MinHeight="120" />
                    </Grid.RowDefinitions>

                    <!-- AppHeader: built-in Minimize / Fullscreen / Close + user info -->
                    <tmpl:AppHeader Grid.Row="0"
                                    DeviceName="{{header}}"
                                    PageTitle="Device Status"
                                    ShowMachineBadge="False" />

                    <!-- Main Device Page -->
                    <ContentControl Grid.Row="1" x:Name="DeviceContent" />

                    <!-- Resizable splitter -->
                    <GridSplitter Grid.Row="2"
                                  HorizontalAlignment="Stretch"
                                  Background="{StaticResource BorderBrush}" />

                    <!-- Log section header: label + PLC status -->
                    <Border Grid.Row="3"
                            Background="{StaticResource DarkBackgroundBrush}"
                            BorderBrush="{StaticResource BorderBrush}"
                            BorderThickness="0,0,0,1"
                            Padding="12,0">
                        <Grid>
                            <TextBlock Text="LIVE LOG"
                                       VerticalAlignment="Center"
                                       FontSize="11" FontWeight="SemiBold"
                                       Foreground="{StaticResource TextSecondaryBrush}" />
                            <core:PlcStatus x:Name="PlcStatusBar"
                                            IsGlobal="True"
                                            HorizontalAlignment="Right"
                                            VerticalAlignment="Center" />
                        </Grid>
                    </Border>

                    <!-- Live Log Viewer -->
                    <core:LiveLogViewer Grid.Row="4" />
                </Grid>
            </Window>
            """;
        WriteFile("MainWindow.xaml", content);
    }

    private void GenerateSinglePageMainWindowXamlCs()
    {
        var ns = _spec.Project.ProjectName;
        var content = $$"""
            using Stackdose.App.DeviceFramework.Pages;
            using Stackdose.App.DeviceFramework.Services;
            using System.Windows;

            namespace {{ns}};

            public partial class MainWindow : Window
            {
                public MainWindow()
                {
                    InitializeComponent();
                    Loaded += OnLoaded;
                }

                private void OnLoaded(object sender, RoutedEventArgs e)
                {
                    var host = new RuntimeHost(projectFolderName: "{{ns}}");
                    var (configs, _) = host.LoadConfigs();
                    if (configs.Count == 0) return;

                    var mapper = new RuntimeMapper();
                    var ctx = mapper.CreateDeviceContext(configs[0]);
                    var page = new DynamicDevicePage();
                    page.SetContext(ctx);
                    DeviceContent.Content = page;
                }
            }
            """;
        WriteFile("MainWindow.xaml.cs", content);
    }

    private void GenerateMainWindowXaml()
    {
        if (IsSinglePage) { GenerateSinglePageMainWindowXaml(); return; }

        var ns = _spec.Project.ProjectName;
        var header = _spec.Project.HeaderDeviceName;
        var content = $"""
            <Window x:Class="{ns}.MainWindow"
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:Custom="http://schemas.stackdose.com/templates"
                    xmlns:TemplatePages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
                    Title="{header}"
                    Height="900" Width="1600"
                    WindowState="Maximized"
                    WindowStyle="None"
                    ResizeMode="CanResize">
                <Custom:MainContainer x:Name="MainShell"
                                      IsShellMode="True"
                                      HeaderDeviceName="{header}"
                                      CurrentMachineDisplayName=""
                                      PageTitle="Machine Overview">
                    <Custom:MainContainer.ShellContent>
                        <TemplatePages:MachineOverviewPage />
                    </Custom:MainContainer.ShellContent>
                </Custom:MainContainer>
            </Window>
            """;
        WriteFile("MainWindow.xaml", content);
    }

    // ═══════════════════════════════════════
    //  MainWindow.xaml.cs
    // ═══════════════════════════════════════

    private void GenerateMainWindowXamlCs()
    {
        if (IsSinglePage) { GenerateSinglePageMainWindowXamlCs(); return; }

        var ns = _spec.Project.ProjectName;
        var isCustomPage = _spec.Project.PageMode.Equals("CustomPage", StringComparison.OrdinalIgnoreCase);
        var shortName = _spec.Project.ShortName;
        var hasSettings = HasPanel("Settings");

        var sb = new StringBuilder();

        // Usings — deduplicate Pages using
        var needsPagesUsing = isCustomPage || hasSettings;
        if (needsPagesUsing)
            sb.AppendLine($"using {ns}.Pages;");
        if (!isCustomPage)
            sb.AppendLine("using Stackdose.App.DeviceFramework.Pages;");

        sb.AppendLine("using Stackdose.App.DeviceFramework.Services;");
        sb.AppendLine($"using {ns}.Handlers;");
        sb.AppendLine("using System.Windows;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("public partial class MainWindow : Window");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly AppController _controller;");
        sb.AppendLine("    private readonly CommandHandlers _handlers = new();");
        sb.AppendLine("    private readonly DataEventHandlers _dataEventHandlers = new();");
        sb.AppendLine();
        sb.AppendLine("    public MainWindow()");
        sb.AppendLine("    {");
        sb.AppendLine("        InitializeComponent();");
        sb.AppendLine();
        sb.AppendLine($"        var runtimeHost = new RuntimeHost(projectFolderName: \"{ns}\");");
        sb.AppendLine("        _controller = new AppController(MainShell, Dispatcher, runtimeHost);");

        if (hasSettings)
        {
            sb.AppendLine();
            sb.AppendLine("        var settingsPage = new SettingsPage();");
            sb.AppendLine("        _controller.SettingsPage = settingsPage;");
            sb.AppendLine("        _controller.OnSettingsNavigating = (page, runtime, machineId) =>");
            sb.AppendLine("        {");
            sb.AppendLine("            if (page is SettingsPage sp)");
            sb.AppendLine("            {");
            sb.AppendLine("                sp.SetMonitorAddresses(runtime.OverviewPage.PlcMonitorAddresses);");
            sb.AppendLine("                sp.SetMachines(runtime.Machines, runtime.ConfigDirectory, machineId);");
            sb.AppendLine("            }");
            sb.AppendLine("        };");
        }

        sb.AppendLine();
        if (isCustomPage)
        {
            sb.AppendLine("        _controller.ConfigurePageFactory(");
            sb.AppendLine("            ctx =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                var page = new {shortName}DevicePage();");
            sb.AppendLine("                page.SetContext(ctx);");
            sb.AppendLine("                return page;");
            sb.AppendLine("            },");
            sb.AppendLine("            (page, ctx) =>");
            sb.AppendLine("            {");
            sb.AppendLine($"                if (page is {shortName}DevicePage p)");
            sb.AppendLine("                    p.SetContext(ctx);");
            sb.AppendLine("            });");
        }
        else
        {
            sb.AppendLine("        _controller.ConfigurePageFactory(");
            sb.AppendLine("            ctx =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var page = new DynamicDevicePage();");
            sb.AppendLine("                page.SetContext(ctx);");
            sb.AppendLine("                page.CommandInterceptor = (machineId, commandName, address) =>");
            sb.AppendLine("                    _handlers.HandleCommand(machineId, commandName, address);");
            sb.AppendLine("                page.DataEventInterceptor = (name, addr, oldVal, newVal) =>");
            sb.AppendLine("                    _dataEventHandlers.HandleEvent(name, addr, oldVal, newVal);");
            sb.AppendLine("                return page;");
            sb.AppendLine("            },");
            sb.AppendLine("            (page, ctx) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                if (page is DynamicDevicePage dp)");
            sb.AppendLine("                {");
            sb.AppendLine("                    dp.SetContext(ctx);");
            sb.AppendLine("                    dp.DataEventInterceptor = (name, addr, oldVal, newVal) =>");
            sb.AppendLine("                        _dataEventHandlers.HandleEvent(name, addr, oldVal, newVal);");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
        }

        sb.AppendLine();
        sb.AppendLine("        Loaded += (_, _) => _controller.Start();");
        sb.AppendLine("        Unloaded += (_, _) => _controller.Dispose();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        WriteFile("MainWindow.xaml.cs", sb.ToString());
    }

    // ═══════════════════════════════════════
    //  Config/app-meta.json
    // ═══════════════════════════════════════

    private void GenerateAppMeta()
    {
        var navItems = new List<object>
        {
            new { title = "Machine Overview", navigationTarget = "MachineOverviewPage", requiredLevel = "Operator" },
            new { title = "Machine Detail", navigationTarget = "MachineDetailPage", requiredLevel = "Operator" },
        };

        foreach (var panel in _spec.Panels.Where(p => p.Position.Equals("Separate", StringComparison.OrdinalIgnoreCase)))
        {
            var target = panel.PanelType switch
            {
                "MaintenanceMode" => "MaintenancePage",
                "Settings" => "SettingsPage",
                _ => panel.PanelType + "Page",
            };
            var title = !string.IsNullOrWhiteSpace(panel.Title) ? panel.Title : panel.PanelType;
            navItems.Add(new { title, navigationTarget = target, requiredLevel = panel.RequiredLevel });
        }

        navItems.Add(new { title = "Log Viewer", navigationTarget = "LogViewerPage", requiredLevel = "Instructor" });
        navItems.Add(new { title = "User Management", navigationTarget = "UserManagementPage", requiredLevel = "Admin" });

        var meta = new
        {
            headerDeviceName = _spec.Project.HeaderDeviceName,
            defaultPageTitle = "Machine Overview",
            useFrameworkShellServices = false,
            enableMetaHotReload = false,
            enableOverviewAlarmCount = true,
            showMachineCards = true,
            showSoftwareInfo = true,
            showLiveLog = true,
            bottomPanelHeight = 440,
            bottomLeftTitle = "Software Information",
            bottomRightTitle = "Live Log",
            navigationItems = navItems.ToArray(),
            softwareInfoItems = new object[]
            {
                new { label = "Application", value = _spec.Project.ProjectName },
                new { label = "Version", value = _spec.Project.Version },
                new { label = "Runtime", value = ".NET 8.0 Windows" },
                new { label = "Mode", value = _spec.Project.PageMode },
            }
        };

        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        WriteFile("Config/app-meta.json", json);
    }

    // ═══════════════════════════════════════
    //  Config/Machine*.config.json
    // ═══════════════════════════════════════

    private void GenerateMachineConfig(MachineInfo machine)
    {
        var machineLabel = BuildMachineFileLabel(machine);
        var commands = _spec.Commands.Where(c => c.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
        var labels = _spec.Labels.Where(l => l.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
        var tags = _spec.Tags.Where(t => t.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();

        var dataEvents = _spec.DataEvents.Where(e => e.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
        var monitorAddresses = BuildMonitorAddresses(machine, commands, labels, dataEvents);

        var commandsDict = new Dictionary<string, string>();
        foreach (var c in commands) commandsDict[c.CommandName] = c.Address;

        var labelsDict = new Dictionary<string, string>();
        foreach (var l in labels) labelsDict[l.LabelName] = l.Address;

        // 視覺樣式（只寫入非預設值，保持 JSON 簡潔）
        var labelStylesDict = new Dictionary<string, object>();
        foreach (var l in labels)
        {
            if (l.FrameShape != "Rectangle" || l.ValueColorTheme != "NeonBlue")
                labelStylesDict[l.LabelName] = new { frameShape = l.FrameShape, valueColorTheme = l.ValueColorTheme };
        }

        var commandStylesDict = new Dictionary<string, object>();
        foreach (var c in commands)
        {
            if (!string.IsNullOrWhiteSpace(c.Theme))
                commandStylesDict[c.CommandName] = new { theme = c.Theme };
        }

        var statusTags = new Dictionary<string, object>();
        statusTags["isRunning"] = new { address = machine.ProcessMonitorIsRunning, type = "bool", access = "read" };
        statusTags["isAlarm"] = new { address = machine.ProcessMonitorIsAlarm, type = "bool", access = "read" };

        var processTags = new Dictionary<string, object>();

        foreach (var tag in tags)
        {
            var tagObj = (object)new { address = tag.Address, type = tag.Type, access = tag.Access, length = tag.Length };
            if (tag.Section.Equals("status", StringComparison.OrdinalIgnoreCase))
                statusTags[tag.TagName] = tagObj;
            else
                processTags[tag.TagName] = tagObj;
        }

        if (!tags.Any(t => t.Section.Equals("process", StringComparison.OrdinalIgnoreCase)))
        {
            processTags["batchNo"] = new { address = "D400", type = "string", access = "read", length = 8 };
            processTags["recipeNo"] = new { address = "D410", type = "string", access = "read", length = 8 };
        }
        if (!tags.Any(t => t.TagName.Equals("heartbeat", StringComparison.OrdinalIgnoreCase)))
            statusTags["heartbeat"] = new { address = "D300", type = "int16", access = "read", length = 1 };

        // ── Module resolution ──────────────────────────────────────────────
        var modules = machine.Modules
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.Trim())
            .ToArray();
        if (modules.Length == 0) modules = ["processControl"];

        bool HasModule(string key) => modules.Any(m => m.Equals(key, StringComparison.OrdinalIgnoreCase));

        // Generate sub-config templates and collect file references
        string? alarmConfigFile = null;
        string? sensorConfigFile = null;
        string[]? printHeadConfigs = null;

        if (HasModule("alarm"))
        {
            alarmConfigFile = $"Config/Machine{machineLabel}/alarms.json";
            GenerateAlarmConfigTemplate(machineLabel);
        }
        if (HasModule("sensors"))
        {
            sensorConfigFile = $"Config/Machine{machineLabel}/sensors.json";
            GenerateSensorConfigTemplate(machineLabel);
        }
        if (HasModule("printHead"))
        {
            printHeadConfigs = [$"Config/Machine{machineLabel}/printhead1.json"];
            GeneratePrintHeadConfigTemplate(machineLabel);
        }

        // ── PlcDeviceEditor panel flag ─────────────────────────────────────
        var showPlcEditor = _spec.Panels.Any(p =>
            p.PanelType.Equals("PlcDeviceEditor", StringComparison.OrdinalIgnoreCase)
            && (p.MachineId == "*" || p.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)));

        // ── Build JSON with optional fields ────────────────────────────────
        var serOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var node = new JsonObject
        {
            ["machine"]        = JsonSerializer.SerializeToNode(new { id = machine.MachineId, name = machine.MachineName, enable = true }, serOpts),
            ["plc"]            = JsonSerializer.SerializeToNode(new { ip = machine.PlcIp, port = machine.PlcPort, pollIntervalMs = machine.PollIntervalMs, autoConnect = _spec.Project.AutoConnect, monitorAddresses }, serOpts),
            ["commands"]       = JsonSerializer.SerializeToNode(commandsDict, serOpts),
            ["processMonitor"] = JsonSerializer.SerializeToNode(new { isRunning = machine.ProcessMonitorIsRunning, isCompleted = machine.ProcessMonitorIsCompleted, isAlarm = machine.ProcessMonitorIsAlarm }, serOpts),
        };

        if (alarmConfigFile  != null) node["alarmConfigFile"]  = alarmConfigFile;
        if (sensorConfigFile != null) node["sensorConfigFile"] = sensorConfigFile;
        if (printHeadConfigs != null) node["printHeadConfigs"] = JsonSerializer.SerializeToNode(printHeadConfigs, serOpts);

        node["detailLabels"] = JsonSerializer.SerializeToNode(labelsDict, serOpts);
        if (labelStylesDict.Count > 0)
            node["detailLabelStyles"] = JsonSerializer.SerializeToNode(labelStylesDict, serOpts);
        if (commandStylesDict.Count > 0)
            node["commandStyles"] = JsonSerializer.SerializeToNode(commandStylesDict, serOpts);
        node["tags"]         = JsonSerializer.SerializeToNode(new { status = statusTags, process = processTags }, serOpts);
        node["modules"]      = JsonSerializer.SerializeToNode(modules, serOpts);
        node["layoutMode"]             = _spec.Project.LayoutMode;
        node["rightColumnWidthStar"]   = _spec.Project.RightColumnWidthStar;
        node["liveDataTitle"]          = _spec.Project.LiveDataTitle;
        node["showPlcEditor"]          = showPlcEditor;
        node["showLiveLog"]            = machine.ShowLiveLog;

        var machineDataEvents = _spec.DataEvents
            .Where(e => e.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase))
            .Select(e => new { name = e.Name, address = e.Address, trigger = e.Trigger, threshold = e.Threshold, dataType = e.DataType })
            .ToArray();
        node["dataEvents"] = JsonSerializer.SerializeToNode(machineDataEvents, serOpts);

        var json = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        WriteFile($"Config/Machine{machineLabel}.config.json", json);
    }

    // ═══════════════════════════════════════
    //  Module Config Templates
    // ═══════════════════════════════════════

    private void GenerateAlarmConfigTemplate(string machineLabel)
    {
        var content = """
            {
              "Alarms": [
                { "Group": "Safety",   "Device": "M100", "Bit": 0, "OperationDescription": "Emergency stop triggered" },
                { "Group": "Safety",   "Device": "M100", "Bit": 1, "OperationDescription": "Door interlock open" },
                { "Group": "Process",  "Device": "M101", "Bit": 0, "OperationDescription": "Process alarm 1" },
                { "Group": "Process",  "Device": "M101", "Bit": 1, "OperationDescription": "Process alarm 2" }
              ]
            }
            """;
        WriteFile($"Config/Machine{machineLabel}/alarms.json", content);
    }

    private void GenerateSensorConfigTemplate(string machineLabel)
    {
        var content = """
            [
              { "Group": "Temperature", "Device": "D100", "Bit": "",  "Value": ">80",  "Mode": "COMPARE", "OperationDescription": "Temperature high warning" },
              { "Group": "Temperature", "Device": "D101", "Bit": "",  "Value": "<10",  "Mode": "COMPARE", "OperationDescription": "Temperature low warning" },
              { "Group": "Safety",      "Device": "M100", "Bit": "0", "Value": "1",    "Mode": "AND",     "OperationDescription": "Emergency stop active" }
            ]
            """;
        WriteFile($"Config/Machine{machineLabel}/sensors.json", content);
    }

    private void GeneratePrintHeadConfigTemplate(string machineLabel)
    {
        var content = """
            {
              "DriverType": "Feiyang",
              "Model": "Feiyang-M1536",
              "Enabled": true,
              "MachineType": "A",
              "HeadIndex": 0,
              "Name": "Head1",
              "BoardIP": "192.168.1.200",
              "BoardPort": 10000,
              "PcIP": "192.168.1.100",
              "PcPort": 10000,
              "Waveform": "",
              "Firmware": {
                "MachineType": "M1536",
                "JetColors": [0, 0, 0, 0],
                "BaseVoltages": [23.5, 23.5, 23.5, 23.5],
                "OffsetVoltages": [0.0, 0.0, 0.0, 0.0],
                "HeatTemperature": 40.0,
                "DisableColumnMask": 0,
                "PrintheadColorCount": 1,
                "InstallDirectionPositive": false,
                "EncoderFunction": 0
              },
              "PrintMode": {
                "PrintDirection": "Bidirection",
                "GratingDpi": 1270,
                "ImageDpi": 600,
                "GrayScale": 0,
                "GrayScaleDrop": 1,
                "ResetEncoder": 1000,
                "LColumnCali": [29.9, 22.3, 7.6],
                "RColumnCali": [7.6, 22.3, 29.9],
                "CaliPixelMM": 8
              }
            }
            """;
        WriteFile($"Config/Machine{machineLabel}/printhead1.json", content);
    }

    // ═══════════════════════════════════════
    //  Handlers/CommandHandlers.cs
    // ═══════════════════════════════════════

    private void GenerateCommandHandlers()
    {
        Directory.CreateDirectory(Path.Combine(_outputRoot, "Handlers"));
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {ns}.Handlers;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 命令處理器 — 回傳 true = 框架寫 PLC，回傳 false = 跳過。");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class CommandHandlers");
        sb.AppendLine("{");
        sb.AppendLine("    public bool HandleCommand(string machineId, string commandName, string address)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (machineId, commandName) switch");
        sb.AppendLine("        {");

        foreach (var machine in _spec.Machines)
            foreach (var cmd in _spec.Commands.Where(c => c.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)))
            {
                var m = $"On{San(machine.MachineId)}{San(cmd.CommandName)}";
                sb.AppendLine($"            (\"{machine.MachineId}\", \"{cmd.CommandName}\") => {m}(machineId, address),");
            }

        sb.AppendLine("            _ => true,");
        sb.AppendLine("        };");
        sb.AppendLine("    }");

        foreach (var machine in _spec.Machines)
        {
            var cmds = _spec.Commands.Where(c => c.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (cmds.Count == 0) continue;
            sb.AppendLine();
            sb.AppendLine($"    // ── {machine.MachineName} ({machine.MachineId}) ──");
            foreach (var cmd in cmds)
            {
                var m = $"On{San(machine.MachineId)}{San(cmd.CommandName)}";
                sb.AppendLine();
                sb.AppendLine($"    /// <summary>{cmd.CommandName} ({cmd.Address})</summary>");
                sb.AppendLine($"    public bool {m}(string machineId, string address)");
                sb.AppendLine("    {");
                sb.AppendLine($"        // TODO: 填入 {cmd.CommandName} 邏輯");
                sb.AppendLine("        return true;");
                sb.AppendLine("    }");
            }
        }

        sb.AppendLine("}");
        WriteFile("Handlers/CommandHandlers.cs", sb.ToString());
    }

    // ═══════════════════════════════════════
    //  Handlers/DataEventHandlers.cs
    // ═══════════════════════════════════════

    private void GenerateDataEventHandlers()
    {
        Directory.CreateDirectory(Path.Combine(_outputRoot, "Handlers"));
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {ns}.Handlers;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// PLC 數據變動事件處理器");
        sb.AppendLine("/// 觸發條件在各機器 Config/*.config.json → dataEvents 定義");
        sb.AppendLine("/// 規則：值未變動不觸發；第一次掃描只記錄初始值，不觸發");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class DataEventHandlers");
        sb.AppendLine("{");
        sb.AppendLine("    public void HandleEvent(string eventName, string address, int oldVal, int newVal)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (eventName)");
        sb.AppendLine("        {");

        foreach (var machine in _spec.Machines)
        {
            var events = _spec.DataEvents.Where(e => e.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (events.Count == 0) continue;
            sb.AppendLine($"            // {machine.MachineName} events:");
            foreach (var ev in events)
            {
                bool isBit = IsBitDataEvent(ev);
                if (isBit)
                    sb.AppendLine($"            case \"{ev.Name}\": {ev.Name}(address, newVal != 0, oldVal != 0); break;");
                else
                    sb.AppendLine($"            case \"{ev.Name}\": {ev.Name}(address, newVal, oldVal); break;");
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");

        foreach (var machine in _spec.Machines)
        {
            var events = _spec.DataEvents.Where(e => e.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase)).ToList();
            if (events.Count == 0) continue;
            sb.AppendLine();
            sb.AppendLine($"    // ── {machine.MachineName} ({machine.MachineId}) ──");
            foreach (var ev in events)
            {
                bool isBit = IsBitDataEvent(ev);
                var triggerDesc = ev.Trigger.ToLowerInvariant() switch
                {
                    "risingedge"  => "上升沿 (0→1)",
                    "fallingedge" => "下降沿 (1→0)",
                    "above"       => $"超過 {ev.Threshold}",
                    "below"       => $"低於 {ev.Threshold}",
                    "equals"      => $"等於 {ev.Threshold}",
                    _             => "數值變動",
                };
                sb.AppendLine();
                sb.AppendLine($"    /// <summary>{ev.Address} {triggerDesc}</summary>");
                if (isBit)
                {
                    sb.AppendLine($"    public void {ev.Name}(string address, bool newVal, bool oldVal)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        // TODO: 在此填寫邏輯");
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    public void {ev.Name}(string address, int newVal, int oldVal)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        // TODO: 在此填寫邏輯");
                    sb.AppendLine("    }");
                }
            }
        }

        sb.AppendLine("}");
        WriteFile("Handlers/DataEventHandlers.cs", sb.ToString());
    }

    private static bool IsBitDataEvent(DataEventInfo ev)
    {
        if (!string.IsNullOrWhiteSpace(ev.DataType))
            return ev.DataType.Equals("bit", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(ev.Address)) return false;
        char prefix = char.ToUpperInvariant(ev.Address.TrimStart()[0]);
        return prefix == 'M' || prefix == 'X' || prefix == 'Y';
    }

    // ═══════════════════════════════════════
    //  Custom Page (PageMode=CustomPage)
    // ═══════════════════════════════════════

    private void GenerateCustomPageXaml()
    {
        var ns = _spec.Project.ProjectName;
        var shortName = _spec.Project.ShortName;
        var content = @"<UserControl x:Class=""" + ns + ".Pages." + shortName + @"DevicePage""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:core=""http://schemas.stackdose.com/wpf"">
    <Grid Margin=""20"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
            <RowDefinition Height=""Auto""/>
        </Grid.RowDefinitions>
        <StackPanel Grid.Row=""0"" Margin=""0,0,0,16"">
            <TextBlock Text=""{Binding MachineName}"" FontSize=""20"" FontWeight=""Bold"" Foreground=""{DynamicResource TextPrimaryBrush}""/>
            <TextBlock Text=""{Binding CurrentProcessStateText}"" FontSize=""14"" Foreground=""{DynamicResource TextSecondaryBrush}"" Margin=""0,4,0,0""/>
        </StackPanel>
        <ItemsControl Grid.Row=""1"" ItemsSource=""{Binding Labels}"">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Margin=""0,0,0,8"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""180""/>
                            <ColumnDefinition Width=""*""/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column=""0"" Text=""{Binding Label}"" Foreground=""{DynamicResource TextSecondaryBrush}""/>
                        <core:PlcLabel Grid.Column=""1"" Address=""{Binding Address}"" FontSize=""16"" Foreground=""{DynamicResource TextPrimaryBrush}""/>
                    </Grid>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        <ItemsControl Grid.Row=""2"" ItemsSource=""{Binding Commands}"">
            <ItemsControl.ItemsPanel><ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate></ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Button Content=""{Binding DisplayName}""
                            Command=""{Binding DataContext.ExecuteCommandCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}""
                            CommandParameter=""{Binding Name}"" Margin=""0,0,8,8"" MinWidth=""100"" Height=""36""/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Grid>
</UserControl>";
        WriteFile($"Pages/{shortName}DevicePage.xaml", content);
    }

    private void GenerateCustomPageXamlCs()
    {
        var ns = _spec.Project.ProjectName;
        var shortName = _spec.Project.ShortName;
        var content = $$"""
            using Stackdose.App.DeviceFramework.Models;
            using Stackdose.App.DeviceFramework.Services;
            using Stackdose.App.DeviceFramework.ViewModels;
            using Stackdose.UI.Core.Helpers;
            using System.Windows;
            using System.Windows.Controls;

            namespace {{ns}}.Pages;

            public partial class {{shortName}}DevicePage : UserControl
            {
                private readonly DevicePageViewModel _viewModel = new();

                public {{shortName}}DevicePage()
                {
                    InitializeComponent();
                    DataContext = _viewModel;
                    Loaded += OnLoaded;
                    Unloaded += OnUnloaded;
                }

                public void SetContext(DeviceContext context) => _viewModel.ApplyDeviceContext(context);

                private void OnLoaded(object sender, RoutedEventArgs e) => PlcEventContext.EventTriggered += OnPlcEvent;
                private void OnUnloaded(object sender, RoutedEventArgs e) => PlcEventContext.EventTriggered -= OnPlcEvent;

                private void OnPlcEvent(object? sender, PlcEventTriggeredEventArgs e)
                {
                    if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => OnPlcEvent(sender, e)); return; }
                    switch (e.EventName)
                    {
                        case ProcessEventNames.Running:   _viewModel.MarkProcessRunning();   break;
                        case ProcessEventNames.Completed: _viewModel.MarkProcessCompleted(); break;
                        case ProcessEventNames.Alarm:     _viewModel.MarkProcessFaulted();   break;
                    }
                }
            }
            """;
        WriteFile($"Pages/{shortName}DevicePage.xaml.cs", content);
    }

    // ═══════════════════════════════════════
    //  Settings Page
    // ═══════════════════════════════════════

    private void GenerateSettingsPageXaml()
    {
        var ns = _spec.Project.ProjectName;
        var content = @"<UserControl x:Class=""" + ns + @".Pages.SettingsPage""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:core=""http://schemas.stackdose.com/wpf""
             Background=""{DynamicResource Surface.Bg.Page}"">
    <Grid Margin=""16"">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""1.2*""/>
            <ColumnDefinition Width=""1.8*""/>
        </Grid.ColumnDefinitions>
        <Grid Grid.Column=""0"" Margin=""0,0,6,0"">
            <Grid.RowDefinitions>
                <RowDefinition Height=""*""/>
                <RowDefinition Height=""Auto""/>
            </Grid.RowDefinitions>
            <GroupBox Grid.Row=""0"" Header=""PLC Address Settings"" Margin=""0,0,0,10"">
                <Grid Margin=""8"" MinHeight=""220"">
                    <Grid.ColumnDefinitions><ColumnDefinition Width=""120""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/>
                        <RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/>
                        <RowDefinition Height=""*""/>
                    </Grid.RowDefinitions>
                    <TextBlock Text=""PLC IP"" VerticalAlignment=""Center"" Grid.Row=""0"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""0"" Grid.Column=""1"" Text=""{Binding PlcIpAddress}"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""PLC Port"" VerticalAlignment=""Center"" Grid.Row=""1"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""1"" Grid.Column=""1"" Text=""{Binding PlcPort}"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""Monitor Map"" VerticalAlignment=""Center"" Grid.Row=""2"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""2"" Grid.Column=""1"" Text=""{Binding MonitorMap}"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""Config Root"" VerticalAlignment=""Center"" Grid.Row=""3"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""3"" Grid.Column=""1"" Text=""{Binding ConfigRootPath}"" IsReadOnly=""True"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""Monitored"" VerticalAlignment=""Top"" Grid.Row=""4"" Grid.Column=""0""/>
                    <ListBox Grid.Row=""4"" Grid.Column=""1"" ItemsSource=""{Binding RegisteredMonitorDeviceItems}"" FontFamily=""Consolas"" FontSize=""11""/>
                </Grid>
            </GroupBox>
            <GroupBox Grid.Row=""1"" Header=""PLC Device Editor"">
                <core:PlcDeviceEditor Margin=""8,0,8,0"" Height=""238"" Label=""Device Editor"" Address=""D200"" Value=""0"" Reason=""Settings manual operation"" RequiredLevel=""SuperAdmin"" EnableAuditTrail=""True""/>
            </GroupBox>
        </Grid>
        <Grid Grid.Column=""1"" Margin=""6,0,0,0"">
            <Grid.RowDefinitions>
                <RowDefinition Height=""Auto""/>
                <RowDefinition Height=""*""/>
            </Grid.RowDefinitions>
            <Border Grid.Row=""0"" Padding=""12"" CornerRadius=""8"" Margin=""0,0,0,10"" Background=""{DynamicResource Surface.Bg.Panel}"" BorderBrush=""{DynamicResource Surface.Border.Default}"" BorderThickness=""1"">
                <StackPanel Orientation=""Horizontal"">
                    <TextBlock Text=""Target Machine: "" VerticalAlignment=""Center"" Margin=""0,0,8,0""/>
                    <ComboBox ItemsSource=""{Binding MachineOptions}"" SelectedValue=""{Binding SelectedMachineId}"" SelectedValuePath=""MachineId"" DisplayMemberPath=""DisplayName"" MinWidth=""200"" Height=""30""/>
                </StackPanel>
            </Border>
            <GroupBox Grid.Row=""1"" Header=""Machine Resources"">
                <Grid Margin=""8"">
                    <Grid.ColumnDefinitions><ColumnDefinition Width=""120""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions>
                    <Grid.RowDefinitions><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/></Grid.RowDefinitions>
                    <TextBlock Text=""Machine Config"" VerticalAlignment=""Center"" Grid.Row=""0"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""0"" Grid.Column=""1"" Text=""{Binding MachineConfigPath}"" IsReadOnly=""True"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""Alarm Config"" VerticalAlignment=""Center"" Grid.Row=""1"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""1"" Grid.Column=""1"" Text=""{Binding AlarmConfigPath}"" IsReadOnly=""True"" Margin=""0,0,0,8""/>
                    <TextBlock Text=""PLC IP"" VerticalAlignment=""Center"" Grid.Row=""2"" Grid.Column=""0""/>
                    <TextBox Grid.Row=""2"" Grid.Column=""1"" Text=""{Binding MachineIpAddress, Mode=OneWay}"" IsReadOnly=""True""/>
                </Grid>
            </GroupBox>
        </Grid>
    </Grid>
</UserControl>";
        WriteFile("Pages/SettingsPage.xaml", content);
    }

    private void GenerateSettingsPageXamlCs()
    {
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();
        sb.AppendLine("using Stackdose.App.DeviceFramework.Models;");
        sb.AppendLine("using Stackdose.UI.Core.Helpers;");
        sb.AppendLine("using System.Collections.ObjectModel;");
        sb.AppendLine("using System.ComponentModel;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Windows.Controls;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns}.Pages;");
        sb.AppendLine();
        sb.AppendLine("public partial class SettingsPage : UserControl, INotifyPropertyChanged");
        sb.AppendLine("{");
        L(sb, "    private string _plcIpAddress = string.Empty;");
        L(sb, "    private string _plcPort = string.Empty;");
        L(sb, "    private string _monitorMap = string.Empty;");
        L(sb, "    private string _configRootPath = string.Empty;");
        L(sb, "    private string _selectedMachineId = string.Empty;");
        L(sb, "    private string _machineConfigPath = string.Empty;");
        L(sb, "    private string _alarmConfigPath = string.Empty;");
        L(sb, "    private string _machineIpAddress = string.Empty;");
        sb.AppendLine();
        L(sb, "    public SettingsPage() { InitializeComponent(); DataContext = this; }");
        sb.AppendLine();
        L(sb, "    public string PlcIpAddress { get => _plcIpAddress; set { _plcIpAddress = value; N(); } }");
        L(sb, "    public string PlcPort { get => _plcPort; set { _plcPort = value; N(); } }");
        L(sb, "    public string MonitorMap { get => _monitorMap; set { _monitorMap = value; N(); } }");
        L(sb, "    public string ConfigRootPath { get => _configRootPath; set { _configRootPath = value; N(); } }");
        L(sb, "    public string SelectedMachineId { get => _selectedMachineId; set { _selectedMachineId = value; N(); } }");
        L(sb, "    public string MachineConfigPath { get => _machineConfigPath; set { _machineConfigPath = value; N(); } }");
        L(sb, "    public string AlarmConfigPath { get => _alarmConfigPath; set { _alarmConfigPath = value; N(); } }");
        L(sb, "    public string MachineIpAddress { get => _machineIpAddress; set { _machineIpAddress = value; N(); } }");
        L(sb, "    public ObservableCollection<MachineOption> MachineOptions { get; } = [];");
        L(sb, "    public ObservableCollection<string> RegisteredMonitorDeviceItems { get; } = [];");
        sb.AppendLine();
        L(sb, "    public void SetMonitorAddresses(string monitorAddresses) => MonitorMap = monitorAddresses;");
        sb.AppendLine();
        L(sb, "    public void SetMachines(IReadOnlyDictionary<string, MachineConfig> machines, string configRootPath, string? defaultMachineId)");
        L(sb, "    {");
        L(sb, "        ConfigRootPath = configRootPath;");
        L(sb, "        MachineOptions.Clear();");
        L(sb, "        foreach (var (id, cfg) in machines)");
        L(sb, "            MachineOptions.Add(new MachineOption(id, cfg.Machine.Name));");
        L(sb, "        if (!string.IsNullOrWhiteSpace(defaultMachineId)) SelectedMachineId = defaultMachineId;");
        L(sb, "        else if (MachineOptions.Count > 0) SelectedMachineId = MachineOptions[0].MachineId;");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    public event PropertyChangedEventHandler? PropertyChanged;");
        L(sb, "    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));");
        L(sb, "}");
        sb.AppendLine();
        L(sb, "public record MachineOption(string MachineId, string DisplayName);");
        WriteFile("Pages/SettingsPage.xaml.cs", sb.ToString());
    }

    // ═══════════════════════════════════════
    //  Maintenance Page
    // ═══════════════════════════════════════

    private void GenerateMaintenancePageXaml()
    {
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();
        sb.Append(@"<UserControl x:Class=""" + ns + @".Pages.MaintenancePage""
             xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             xmlns:core=""http://schemas.stackdose.com/wpf""
             Background=""{DynamicResource Surface.Bg.Page}"">
    <Grid Margin=""16"">
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto""/>
            <RowDefinition Height=""*""/>
        </Grid.RowDefinitions>
        <Border Grid.Row=""0"" Padding=""12"" CornerRadius=""8"" Margin=""0,0,0,12""
                Background=""{DynamicResource Surface.Bg.Panel}""
                BorderBrush=""{DynamicResource Surface.Border.Default}"" BorderThickness=""1"">
            <StackPanel Orientation=""Horizontal"">
                <TextBlock Text=""MAINTENANCE MODE"" FontSize=""14"" FontWeight=""Bold"" VerticalAlignment=""Center"" Margin=""0,0,20,0"" Foreground=""{DynamicResource Accent.Warning}""/>
                <TextBlock Text=""Target Machine: "" VerticalAlignment=""Center"" Margin=""0,0,8,0""/>
                <ComboBox x:Name=""CboMachine"" ItemsSource=""{Binding MachineIds}"" SelectedItem=""{Binding SelectedMachineId}"" MinWidth=""200"" Height=""30"" SelectionChanged=""OnMachineChanged""/>
            </StackPanel>
        </Border>
        <ScrollViewer Grid.Row=""1"" VerticalScrollBarVisibility=""Auto"">
            <StackPanel x:Name=""ItemsPanel"">
");

        foreach (var machine in _spec.Machines)
        {
            var items = _spec.MaintenanceItems
                .Where(i => i.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase) || i.MachineId == "*")
                .ToList();
            if (items.Count == 0) continue;

            sb.AppendLine($"                <Border x:Name=\"Panel_{machine.MachineId}\" Visibility=\"Collapsed\" CornerRadius=\"8\" Padding=\"12\" Margin=\"4\" Background=\"{{DynamicResource Surface.Bg.Panel}}\" BorderBrush=\"{{DynamicResource Surface.Border.Default}}\" BorderThickness=\"1\">");
            sb.AppendLine($"                    <StackPanel>");
            sb.AppendLine($"                        <TextBlock Text=\"{Esc(machine.MachineName)}\" FontSize=\"16\" FontWeight=\"Bold\" Margin=\"0,0,0,12\" Foreground=\"{{DynamicResource TextPrimaryBrush}}\"/>");
            sb.AppendLine($"                        <WrapPanel>");

            foreach (var item in items)
            {
                var lbl = Esc(item.Label);
                var addr = item.Address;
                var name = Esc(item.ItemName);
                switch (item.Type.ToLowerInvariant())
                {
                    case "editor":
                        sb.AppendLine($"                            <GroupBox Header=\"{lbl}\" Margin=\"4\" MinWidth=\"280\">");
                        sb.AppendLine($"                                <core:PlcDeviceEditor Label=\"{lbl}\" Address=\"{addr}\" Value=\"0\" Reason=\"Maintenance: {name}\" RequiredLevel=\"Supervisor\" EnableAuditTrail=\"True\" Height=\"180\"/>");
                        sb.AppendLine($"                            </GroupBox>");
                        break;
                    case "toggle":
                        sb.AppendLine($"                            <Border CornerRadius=\"6\" Padding=\"12,8\" Margin=\"4\" MinWidth=\"160\" Background=\"{{DynamicResource Surface.Bg.Card}}\">");
                        sb.AppendLine($"                                <StackPanel>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{lbl}\" FontSize=\"11\" Foreground=\"{{DynamicResource TextSecondaryBrush}}\"/>");
                        sb.AppendLine($"                                    <StackPanel Orientation=\"Horizontal\" Margin=\"0,6,0,0\">");
                        sb.AppendLine($"                                        <core:SecuredButton Content=\"ON\" Tag=\"{addr},1\" Click=\"OnToggleClick\" Theme=\"Success\" RequiredLevel=\"Supervisor\" MinWidth=\"55\" Margin=\"0,0,4,0\"/>");
                        sb.AppendLine($"                                        <core:SecuredButton Content=\"OFF\" Tag=\"{addr},0\" Click=\"OnToggleClick\" Theme=\"Error\" RequiredLevel=\"Supervisor\" MinWidth=\"55\"/>");
                        sb.AppendLine($"                                    </StackPanel>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{addr}\" FontSize=\"9\" Margin=\"0,4,0,0\" Foreground=\"{{DynamicResource TextTertiaryBrush}}\"/>");
                        sb.AppendLine($"                                </StackPanel>");
                        sb.AppendLine($"                            </Border>");
                        break;
                    case "momentary":
                        sb.AppendLine($"                            <Border CornerRadius=\"6\" Padding=\"12,8\" Margin=\"4\" MinWidth=\"160\" Background=\"{{DynamicResource Surface.Bg.Card}}\">");
                        sb.AppendLine($"                                <StackPanel>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{lbl}\" FontSize=\"11\" Foreground=\"{{DynamicResource TextSecondaryBrush}}\"/>");
                        sb.AppendLine($"                                    <core:SecuredButton Content=\"{name}\" Tag=\"{addr}\" PreviewMouseLeftButtonDown=\"OnMomentaryDown\" PreviewMouseLeftButtonUp=\"OnMomentaryUp\" Theme=\"Warning\" RequiredLevel=\"Supervisor\" MinWidth=\"120\" Margin=\"0,6,0,0\"/>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{addr}\" FontSize=\"9\" Margin=\"0,4,0,0\" Foreground=\"{{DynamicResource TextTertiaryBrush}}\"/>");
                        sb.AppendLine($"                                </StackPanel>");
                        sb.AppendLine($"                            </Border>");
                        break;
                    default: // readonly
                        sb.AppendLine($"                            <Border CornerRadius=\"6\" Padding=\"12,8\" Margin=\"4\" MinWidth=\"160\" Background=\"{{DynamicResource Surface.Bg.Card}}\">");
                        sb.AppendLine($"                                <StackPanel>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{lbl}\" FontSize=\"11\" Foreground=\"{{DynamicResource TextSecondaryBrush}}\"/>");
                        sb.AppendLine($"                                    <core:PlcLabel Address=\"{addr}\" FontSize=\"20\" FontWeight=\"Bold\" DefaultValue=\"--\" Margin=\"0,4,0,0\" Foreground=\"{{DynamicResource TextPrimaryBrush}}\"/>");
                        sb.AppendLine($"                                    <TextBlock Text=\"{addr}\" FontSize=\"9\" Margin=\"0,4,0,0\" Foreground=\"{{DynamicResource TextTertiaryBrush}}\"/>");
                        sb.AppendLine($"                                </StackPanel>");
                        sb.AppendLine($"                            </Border>");
                        break;
                }
            }

            sb.AppendLine("                        </WrapPanel>");
            sb.AppendLine("                    </StackPanel>");
            sb.AppendLine("                </Border>");
        }

        sb.Append(@"            </StackPanel>
        </ScrollViewer>
    </Grid>
</UserControl>");
        WriteFile("Pages/MaintenancePage.xaml", sb.ToString());
    }

    private void GenerateMaintenancePageXamlCs()
    {
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();
        L(sb, "using Stackdose.UI.Core.Helpers;");
        L(sb, "using System.Collections.ObjectModel;");
        L(sb, "using System.ComponentModel;");
        L(sb, "using System.Runtime.CompilerServices;");
        L(sb, "using System.Windows;");
        L(sb, "using System.Windows.Controls;");
        L(sb, "using System.Windows.Input;");
        sb.AppendLine();
        L(sb, $"namespace {ns}.Pages;");
        sb.AppendLine();
        L(sb, "public partial class MaintenancePage : UserControl, INotifyPropertyChanged");
        L(sb, "{");
        L(sb, "    private string _selectedMachineId = string.Empty;");
        sb.AppendLine();
        L(sb, "    public MaintenancePage()");
        L(sb, "    {");
        L(sb, "        InitializeComponent();");
        L(sb, "        DataContext = this;");
        foreach (var m in _spec.Machines)
            L(sb, $"        MachineIds.Add(\"{m.MachineId}\");");
        L(sb, "        if (MachineIds.Count > 0) SelectedMachineId = MachineIds[0];");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    public ObservableCollection<string> MachineIds { get; } = [];");
        L(sb, "    public string SelectedMachineId");
        L(sb, "    {");
        L(sb, "        get => _selectedMachineId;");
        L(sb, "        set { _selectedMachineId = value; N(); ShowMachinePanel(value); }");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    private void ShowMachinePanel(string machineId)");
        L(sb, "    {");
        foreach (var m in _spec.Machines)
        {
            L(sb, $"        if (FindName(\"Panel_{m.MachineId}\") is Border b{San(m.MachineId)})");
            L(sb, $"            b{San(m.MachineId)}.Visibility = machineId == \"{m.MachineId}\" ? Visibility.Visible : Visibility.Collapsed;");
        }
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    private void OnMachineChanged(object sender, SelectionChangedEventArgs e) { }");
        sb.AppendLine();
        L(sb, "    private async void OnToggleClick(object sender, RoutedEventArgs e)");
        L(sb, "    {");
        L(sb, "        if (sender is not FrameworkElement fe || fe.Tag is not string tagStr) return;");
        L(sb, "        var manager = PlcContext.GlobalStatus?.CurrentManager;");
        L(sb, "        if (manager is null || !manager.IsConnected) return;");
        L(sb, "        await manager.WriteAsync(tagStr);");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    private async void OnMomentaryDown(object sender, MouseButtonEventArgs e)");
        L(sb, "    {");
        L(sb, "        if (sender is not FrameworkElement fe || fe.Tag is not string addr) return;");
        L(sb, "        var manager = PlcContext.GlobalStatus?.CurrentManager;");
        L(sb, "        if (manager is null || !manager.IsConnected) return;");
        L(sb, "        await manager.WriteAsync($\"{addr},1\");");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    private async void OnMomentaryUp(object sender, MouseButtonEventArgs e)");
        L(sb, "    {");
        L(sb, "        if (sender is not FrameworkElement fe || fe.Tag is not string addr) return;");
        L(sb, "        var manager = PlcContext.GlobalStatus?.CurrentManager;");
        L(sb, "        if (manager is null || !manager.IsConnected) return;");
        L(sb, "        await manager.WriteAsync($\"{addr},0\");");
        L(sb, "    }");
        sb.AppendLine();
        L(sb, "    public event PropertyChangedEventHandler? PropertyChanged;");
        L(sb, "    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));");
        L(sb, "}");
        WriteFile("Pages/MaintenancePage.xaml.cs", sb.ToString());
    }

    // ═══════════════════════════════════════
    //  Handlers/MaintenanceHandlers.cs
    // ═══════════════════════════════════════

    private void GenerateMaintenanceHandlers()
    {
        Directory.CreateDirectory(Path.Combine(_outputRoot, "Handlers"));
        var ns = _spec.Project.ProjectName;
        var sb = new StringBuilder();
        L(sb, $"namespace {ns}.Handlers;");
        sb.AppendLine();
        L(sb, "/// <summary>維護模式處理器 — 回傳 true = 允許操作。</summary>");
        L(sb, "public sealed class MaintenanceHandlers");
        L(sb, "{");

        foreach (var machine in _spec.Machines)
        {
            var items = _spec.MaintenanceItems
                .Where(i => i.MachineId.Equals(machine.MachineId, StringComparison.OrdinalIgnoreCase) || i.MachineId == "*")
                .ToList();
            if (items.Count == 0) continue;
            sb.AppendLine();
            L(sb, $"    // ── {machine.MachineName} ({machine.MachineId}) ──");
            foreach (var item in items)
            {
                var m = $"On{San(machine.MachineId)}{San(item.ItemName)}";
                sb.AppendLine();
                L(sb, $"    /// <summary>{item.Label} ({item.Address}, {item.Type})</summary>");
                L(sb, $"    public bool {m}(string machineId, string address)");
                L(sb, "    {");
                L(sb, $"        // TODO: 填入 {item.Label} 的安全檢查邏輯");
                L(sb, "        return true;");
                L(sb, "    }");
            }
        }

        L(sb, "}");
        WriteFile("Handlers/MaintenanceHandlers.cs", sb.ToString());
    }

    // ═══════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════

    private static void L(StringBuilder sb, string line) => sb.AppendLine(line);
    private static string San(string name) => SanitizeIdentifier(name);
    private static string Esc(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string[] BuildMonitorAddresses(MachineInfo machine, List<CommandInfo> commands, List<LabelInfo> labels, List<DataEventInfo>? dataEvents = null)
    {
        var dAddresses = new SortedSet<int>();
        var mAddresses = new SortedSet<int>();

        void AddAddress(string addr)
        {
            if (string.IsNullOrWhiteSpace(addr) || addr == "--") return;
            var upper = addr.Trim().ToUpperInvariant();
            if (upper.StartsWith('D') && int.TryParse(upper[1..], out var dn)) dAddresses.Add(dn);
            else if (upper.StartsWith('M') && int.TryParse(upper[1..], out var mn)) mAddresses.Add(mn);
        }

        AddAddress(machine.ProcessMonitorIsRunning);
        AddAddress(machine.ProcessMonitorIsCompleted);
        AddAddress(machine.ProcessMonitorIsAlarm);
        foreach (var c in commands) AddAddress(c.Address);
        foreach (var l in labels) AddAddress(l.Address);
        if (dataEvents != null)
            foreach (var e in dataEvents) AddAddress(e.Address);

        var result = new List<string>();
        if (dAddresses.Count > 0) result.Add($"D{dAddresses.Min},{dAddresses.Max - dAddresses.Min + 1}");
        if (mAddresses.Count > 0) result.Add($"M{mAddresses.Min},{mAddresses.Max - mAddresses.Min + 1}");
        return result.Count > 0 ? [.. result] : ["D0,1"];
    }

    private static string BuildMachineFileLabel(MachineInfo machine)
    {
        var firstName = machine.MachineName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? machine.MachineId;
        var digitMatch = Regex.Match(machine.MachineId, @"\d+$");
        return $"{firstName}{(digitMatch.Success ? digitMatch.Value : "1")}";
    }

    private static string SanitizeIdentifier(string name)
    {
        var cleaned = Regex.Replace(name, @"[^a-zA-Z0-9]", "");
        if (cleaned.Length == 0) return "Unknown";
        return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_outputRoot, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        _generatedFiles.Add(relativePath);
    }
}
