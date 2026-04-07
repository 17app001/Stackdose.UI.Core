using Microsoft.Win32;
using Stackdose.Tools.ProjectGenerator;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;

namespace Stackdose.Tools.ProjectGeneratorUI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    // ── Project ───────────────────────────────────────────────────────────
    private string _projectName            = "Stackdose.App.MyDevice";
    private string _headerDeviceName       = "MY DEVICE";
    private string _version                = "v1.0.0";
    private string _pageMode               = "DynamicDevicePage";
    private string _layoutMode             = "SplitRight";
    private bool   _autoConnect            = false;
    private double _rightColumnWidthStar   = 0.85;
    private int    _leftCommandWidthPx     = 250;
    private string _liveDataTitle          = "Live Data";
    private string _deviceStatusTitle      = "Device Status";

    public string ProjectName           { get => _projectName;          set { _projectName          = value; N(); } }
    public string HeaderDeviceName      { get => _headerDeviceName;     set { _headerDeviceName     = value; N(); } }
    public string Version               { get => _version;              set { _version              = value; N(); } }
    public string PageMode              { get => _pageMode;             set { _pageMode             = value; N(); } }
    public string LayoutMode            { get => _layoutMode;           set { _layoutMode           = value; N(); } }
    public bool   AutoConnect           { get => _autoConnect;          set { _autoConnect          = value; N(); } }
    public double RightColumnWidthStar  { get => _rightColumnWidthStar; set { _rightColumnWidthStar = value; N(); } }
    public int    LeftCommandWidthPx    { get => _leftCommandWidthPx;   set { _leftCommandWidthPx   = value; N(); } }
    public string LiveDataTitle         { get => _liveDataTitle;        set { _liveDataTitle        = value; N(); } }
    public string DeviceStatusTitle     { get => _deviceStatusTitle;    set { _deviceStatusTitle    = value; N(); } }

    public string[] PageModes   { get; } = ["DynamicDevicePage", "SinglePage", "CustomPage"];
    public string[] LayoutModes { get; } = ["SplitRight", "Standard", "SplitBottom"];

    // ── Machines ─────────────────────────────────────────────────────────
    public ObservableCollection<MachineViewModel> Machines { get; } = [];

    private MachineViewModel? _selectedMachine;
    public MachineViewModel? SelectedMachine
    {
        get => _selectedMachine;
        set { _selectedMachine = value; N(); N(nameof(HasSelectedMachine)); }
    }
    public bool HasSelectedMachine => _selectedMachine != null;

    // ── Panels ────────────────────────────────────────────────────────────
    private bool _hasMaintenanceMode;
    private bool _hasSettings;
    private bool _hasPlcDeviceEditor;
    private string _maintenanceLevel    = "Supervisor";
    private string _settingsLevel       = "Admin";
    private string _plcDeviceEditorLevel = "Supervisor";

    public bool HasMaintenanceMode    { get => _hasMaintenanceMode;    set { _hasMaintenanceMode    = value; N(); } }
    public bool HasSettings           { get => _hasSettings;           set { _hasSettings           = value; N(); } }
    public bool HasPlcDeviceEditor    { get => _hasPlcDeviceEditor;    set { _hasPlcDeviceEditor    = value; N(); } }
    public string MaintenanceLevel    { get => _maintenanceLevel;      set { _maintenanceLevel      = value; N(); } }
    public string SettingsLevel       { get => _settingsLevel;         set { _settingsLevel         = value; N(); } }
    public string PlcDeviceEditorLevel { get => _plcDeviceEditorLevel; set { _plcDeviceEditorLevel = value; N(); } }

    public string[] AccessLevels { get; } = ["Operator", "Instructor", "Supervisor", "Admin", "SuperAdmin"];

    // ── Maintenance items ────────────────────────────────────────────────
    public ObservableCollection<MaintenanceItemRow> MaintenanceItems { get; } = [];

    // ── Output ────────────────────────────────────────────────────────────
    private string _outputPath = FindSolutionFile(AppDomain.CurrentDomain.BaseDirectory) is { } slnFile
        ? Path.GetDirectoryName(slnFile)!
        : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private string _generationLog = string.Empty;
    private bool   _isGenerating;

    public string OutputPath     { get => _outputPath;     set { _outputPath     = value; N(); } }
    public string GenerationLog  { get => _generationLog;  set { _generationLog  = value; N(); } }
    public bool   IsGenerating   { get => _isGenerating;   set { _isGenerating   = value; N(); } }

    // ── Commands ──────────────────────────────────────────────────────────
    public ICommand AddMachineCmd         { get; }
    public ICommand RemoveMachineCmd      { get; }
    public ICommand AddCommandCmd         { get; }
    public ICommand RemoveCommandCmd      { get; }
    public ICommand AddLabelCmd           { get; }
    public ICommand RemoveLabelCmd        { get; }
    public ICommand AddMaintenanceItemCmd { get; }
    public ICommand RemoveMaintenanceItemCmd { get; }
    public ICommand AddDataEventCmd       { get; }
    public ICommand RemoveDataEventCmd    { get; }
    public ICommand ImportDataEventsCmd   { get; }
    public ICommand BrowseOutputCmd       { get; }
    public ICommand GenerateCmd           { get; }
    public ICommand SaveSpecCmd              { get; }
    public ICommand LoadSpecCmd              { get; }
    public ICommand ApplyLayoutPresetCmd     { get; }
    public ICommand AddStatusLabelCmd        { get; }
    public ICommand RemoveStatusLabelCmd     { get; }

    public MainViewModel()
    {
        AddMachineCmd    = new RelayCommand(_ => AddMachine());
        RemoveMachineCmd = new RelayCommand(_ => RemoveMachine(), _ => HasSelectedMachine);
        AddCommandCmd    = new RelayCommand(_ => AddCommand(),    _ => HasSelectedMachine);
        RemoveCommandCmd = new RelayCommand<CommandRow>(RemoveCommand, r => r != null);
        AddLabelCmd      = new RelayCommand(_ => AddLabel(),      _ => HasSelectedMachine);
        RemoveLabelCmd   = new RelayCommand<LabelRow>(RemoveLabel, r => r != null);
        AddMaintenanceItemCmd    = new RelayCommand(_ => MaintenanceItems.Add(new()));
        RemoveMaintenanceItemCmd = new RelayCommand<MaintenanceItemRow>(r => { if (r != null) MaintenanceItems.Remove(r); }, r => r != null);
        AddDataEventCmd    = new RelayCommand(_ => AddDataEvent(),       _ => HasSelectedMachine);
        RemoveDataEventCmd = new RelayCommand<DataEventRow>(RemoveDataEvent, r => r != null);
        ImportDataEventsCmd = new RelayCommand(_ => ImportDataEvents(),  _ => HasSelectedMachine);
        BrowseOutputCmd  = new RelayCommand(_ => BrowseOutput());
        GenerateCmd      = new RelayCommand(_ => Generate(), _ => !IsGenerating);
        SaveSpecCmd            = new RelayCommand(_ => SaveSpec());
        LoadSpecCmd            = new RelayCommand(_ => LoadSpec());
        ApplyLayoutPresetCmd   = new RelayCommand<string>(ApplyLayoutPreset);
        AddStatusLabelCmd    = new RelayCommand(_ => AddStatusLabel(),           _ => HasSelectedMachine);
        RemoveStatusLabelCmd = new RelayCommand<LabelRow>(RemoveStatusLabel, r => r != null);

        // Add a default machine
        AddMachine();
    }

    private void AddMachine()
    {
        var idx = Machines.Count + 1;
        var vm = new MachineViewModel
        {
            MachineId   = $"M{idx}",
            MachineName = $"Machine {idx}",
        };
        Machines.Add(vm);
        SelectedMachine = vm;
    }

    private void RemoveMachine()
    {
        if (SelectedMachine == null) return;
        var idx = Machines.IndexOf(SelectedMachine);
        Machines.Remove(SelectedMachine);
        SelectedMachine = Machines.Count > 0 ? Machines[Math.Max(0, idx - 1)] : null;
    }

    private void AddCommand()
    {
        if (SelectedMachine == null) return;
        var name = NextName(SelectedMachine.Commands.Select(c => c.Name), "Start");
        var addr = NextAddress(SelectedMachine.Commands.Select(c => c.Address), "M300");
        SelectedMachine.Commands.Add(new CommandRow { Name = name, Address = addr });
    }

    private void RemoveCommand(CommandRow? row)
    {
        if (row == null || SelectedMachine == null) return;
        SelectedMachine.Commands.Remove(row);
    }

    private void AddLabel()
    {
        if (SelectedMachine == null) return;
        var name = NextName(SelectedMachine.Labels.Select(l => l.Name), "Label");
        var addr = NextAddress(SelectedMachine.Labels.Select(l => l.Address), "D100");
        SelectedMachine.Labels.Add(new LabelRow { Name = name, Address = addr });
    }

    private void RemoveLabel(LabelRow? row)
    {
        if (row == null || SelectedMachine == null) return;
        SelectedMachine.Labels.Remove(row);
    }

    private void AddDataEvent()
    {
        if (SelectedMachine == null) return;
        var name = NextName(SelectedMachine.DataEvents.Select(d => d.Name), "OnEvent");
        var addr = NextAddress(SelectedMachine.DataEvents.Select(d => d.Address), "M200");
        SelectedMachine.DataEvents.Add(new DataEventRow { Name = name, Address = addr, Trigger = "changed" });
    }

    private static string NextName(IEnumerable<string> existing, string baseName)
    {
        var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        int i = 1;
        while (set.Contains(baseName + i)) i++;
        return baseName + i;
    }

    /// <summary>
    /// 取現有地址清單的最後一筆，解析前綴+數字後回傳 +1 的地址。
    /// 例：["M300","M301"] → "M302"；空清單或無法解析則回傳 defaultAddress。
    /// </summary>
    private static string NextAddress(IEnumerable<string> existing, string defaultAddress)
    {
        var last = existing.LastOrDefault();
        if (last == null) return defaultAddress;

        var match = System.Text.RegularExpressions.Regex.Match(last, @"^([A-Za-z]+)(\d+)$");
        if (!match.Success) return defaultAddress;

        var prefix = match.Groups[1].Value;
        var num    = int.Parse(match.Groups[2].Value);
        return prefix + (num + 1);
    }

    private void RemoveDataEvent(DataEventRow? row)
    {
        if (row == null || SelectedMachine == null) return;
        SelectedMachine.DataEvents.Remove(row);
    }

    private void ImportDataEvents()
    {
        if (SelectedMachine == null) return;

        var machineId = SelectedMachine.MachineId;
        var machineName = SelectedMachine.MachineName;
        var firstName = machineName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? machineId;
        var digitMatch = System.Text.RegularExpressions.Regex.Match(machineId, @"\d+$");
        var machineLabel = $"{firstName}{(digitMatch.Success ? digitMatch.Value : "1")}";

        var configDir = Path.Combine(OutputPath, ProjectName, "Config", $"Machine{machineLabel}");
        var alarmsPath  = Path.Combine(configDir, "alarms.json");
        var sensorsPath = Path.Combine(configDir, "sensors.json");

        int imported = 0;

        if (File.Exists(alarmsPath))
        {
            try
            {
                var json = File.ReadAllText(alarmsPath);
                var root = JsonSerializer.Deserialize<AlarmImportRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (root?.Alarms != null)
                {
                    foreach (var alarm in root.Alarms)
                    {
                        var name = "On" + SanitizeName(alarm.OperationDescription);
                        SelectedMachine.DataEvents.Add(new DataEventRow
                        {
                            Name = name, Address = alarm.Device, Trigger = "risingEdge", DataType = "bit"
                        });
                        imported++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入 alarms.json 失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        if (File.Exists(sensorsPath))
        {
            try
            {
                var json = File.ReadAllText(sensorsPath);
                var sensors = JsonSerializer.Deserialize<List<SensorImportItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (sensors != null)
                {
                    foreach (var sensor in sensors)
                    {
                        var label = !string.IsNullOrWhiteSpace(sensor.OperationDescription) ? sensor.OperationDescription : sensor.Label ?? sensor.Device;
                        var name = "On" + SanitizeName(label) + "Changed";
                        SelectedMachine.DataEvents.Add(new DataEventRow
                        {
                            Name = name, Address = sensor.Device, Trigger = "changed", DataType = "bit"
                        });
                        imported++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入 sensors.json 失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        if (imported == 0)
            MessageBox.Show($"找不到 Config 檔案或無資料可匯入。\n預期路徑：{configDir}", "匯入提示", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show($"已匯入 {imported} 筆事件。", "匯入完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string SanitizeName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Event";
        // PascalCase: split words, capitalize each, remove special chars
        var words = System.Text.RegularExpressions.Regex.Split(input.Trim(), @"[\s\-_]+");
        var sb = new System.Text.StringBuilder();
        foreach (var w in words)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(w, @"[^a-zA-Z0-9]", "");
            if (cleaned.Length > 0)
                sb.Append(char.ToUpperInvariant(cleaned[0]) + cleaned[1..]);
        }
        return sb.Length > 0 ? sb.ToString() : "Event";
    }

    private sealed class AlarmImportRoot  { public List<AlarmImportItem>? Alarms { get; set; } }
    private sealed class AlarmImportItem  { public string Device { get; set; } = ""; public int Bit { get; set; } public string OperationDescription { get; set; } = ""; }
    private sealed class SensorImportItem { public string Device { get; set; } = ""; public string? Label { get; set; } public string? OperationDescription { get; set; } }

    private void AddStatusLabel()
    {
        if (SelectedMachine == null) return;
        var name = NextName(SelectedMachine.StatusLabels.Select(l => l.Name), "Status");
        SelectedMachine.StatusLabels.Add(new LabelRow { Name = name, Address = NextAddress(SelectedMachine.StatusLabels.Select(l => l.Address), "D500") });
    }

    private void RemoveStatusLabel(LabelRow? row)
    {
        if (row != null) SelectedMachine?.StatusLabels.Remove(row);
    }

    private void ApplyLayoutPreset(string? preset)
    {
        switch (preset)
        {
            case "compact":      LeftCommandWidthPx = 200; RightColumnWidthStar = 0.70; break;
            case "standard":     LeftCommandWidthPx = 250; RightColumnWidthStar = 0.85; break;
            case "wide":         LeftCommandWidthPx = 340; RightColumnWidthStar = 1.00; break;
            case "wide-viewer":  LeftCommandWidthPx = 250; RightColumnWidthStar = 1.40; break;
        }
    }

    private void BrowseOutput()
    {
        var dlg = new OpenFolderDialog { Title = "選擇輸出資料夾", InitialDirectory = OutputPath };
        if (dlg.ShowDialog() == true)
            OutputPath = dlg.FolderName;
    }

    private void Generate()
    {
        if (Machines.Count == 0)
        {
            MessageBox.Show("請至少新增一台設備。", "無法產生", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            MessageBox.Show("請填入專案名稱。", "無法產生", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsGenerating = true;
        GenerationLog = string.Empty;

        try
        {
            var spec = BuildSpec();
            var generator = new ProjectGenerator.ProjectGenerator(spec, OutputPath);
            generator.Generate();

            // 自動備份 spec 到專案資料夾
            var specBackupPath = Path.Combine(OutputPath, spec.Project.ProjectName, spec.Project.ProjectName + ".spec.json");
            File.WriteAllText(specBackupPath, JsonSerializer.Serialize(BuildSpecDto(), _jsonOpts), System.Text.Encoding.UTF8);

            // 自動加入方案
            var csprojPath = Path.Combine(OutputPath, spec.Project.ProjectName, $"{spec.Project.ProjectName}.csproj");
            var slnPath    = FindSolutionFile(AppDomain.CurrentDomain.BaseDirectory);
            var slnResult  = slnPath != null
                ? AddProjectToSolution(slnPath, csprojPath)
                : "⚠️ 未找到 .sln，請手動加入專案";

            var log = new System.Text.StringBuilder();
            log.AppendLine($"✅ 專案產生成功！");
            log.AppendLine($"📁 {Path.Combine(OutputPath, spec.Project.ProjectName)}/");
            log.AppendLine(slnResult);
            log.AppendLine();
            foreach (var f in generator.GeneratedFiles)
                log.AppendLine($"   • {f}");
            log.AppendLine($"   • {spec.Project.ProjectName}.spec.json  ← spec 備份");
            log.AppendLine();
            log.AppendLine("下一步：F5 執行");

            GenerationLog = log.ToString();
        }
        catch (Exception ex)
        {
            GenerationLog = $"❌ 錯誤：{ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private static string? FindSolutionFile(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var slnFiles = dir.GetFiles("*.sln");
            if (slnFiles.Length > 0) return slnFiles[0].FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string AddProjectToSolution(string slnPath, string csprojPath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                Arguments             = $"sln \"{slnPath}\" add \"{csprojPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(10_000);
            return proc.ExitCode == 0
                ? $"🔗 已加入方案：{Path.GetFileName(slnPath)}"
                : $"⚠️ 加入方案失敗：{(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr).Trim()}";
        }
        catch (Exception ex)
        {
            return $"⚠️ 加入方案失敗：{ex.Message}";
        }
    }

    private DeviceSpec BuildSpec()
    {
        var spec = new DeviceSpec
        {
            Project = new ProjectInfo
            {
                ProjectName      = ProjectName.Trim(),
                HeaderDeviceName = HeaderDeviceName.Trim(),
                Version          = Version.Trim(),
                PageMode               = PageMode,
                LayoutMode             = LayoutMode,
                AutoConnect            = AutoConnect,
                RightColumnWidthStar   = RightColumnWidthStar,
                LeftCommandWidthPx     = LeftCommandWidthPx,
                LiveDataTitle          = LiveDataTitle,
                DeviceStatusTitle      = DeviceStatusTitle,
            }
        };

        foreach (var m in Machines)
        {
            spec.Machines.Add(new MachineInfo
            {
                MachineId                = m.MachineId.Trim(),
                MachineName              = m.MachineName.Trim(),
                PlcIp                    = m.PlcIp.Trim(),
                PlcPort                  = m.PlcPort,
                PollIntervalMs           = m.PollMs,
                ProcessMonitorIsRunning  = m.IsRunning.Trim(),
                ProcessMonitorIsCompleted = m.IsCompleted.Trim(),
                ProcessMonitorIsAlarm    = m.IsAlarm.Trim(),
                Modules                  = m.ModulesString,
                ShowLiveLog              = m.ShowLiveLog,
                MachineDesignFile        = m.MachineDesignFile,
            });

            foreach (var c in m.Commands)
                spec.Commands.Add(new CommandInfo { MachineId = m.MachineId, CommandName = c.Name, Address = c.Address, Theme = c.Theme });

            foreach (var l in m.Labels)
                spec.Labels.Add(new LabelInfo { MachineId = m.MachineId, LabelName = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme });
            foreach (var l in m.StatusLabels)
                spec.StatusLabels.Add(new LabelInfo { MachineId = m.MachineId, LabelName = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme });

            foreach (var de in m.DataEvents)
                spec.DataEvents.Add(new DataEventInfo { MachineId = m.MachineId, Name = de.Name, Address = de.Address, Trigger = de.Trigger, Threshold = de.Threshold, DataType = de.DataType });
        }

        if (HasMaintenanceMode)
        {
            spec.Panels.Add(new PanelInfo { PanelType = "MaintenanceMode", MachineId = "*", Position = "Separate", Title = "Maintenance Mode", RequiredLevel = MaintenanceLevel });
            foreach (var item in MaintenanceItems)
                spec.MaintenanceItems.Add(new MaintenanceItemInfo { MachineId = item.MachineId, ItemName = item.ItemName, Address = item.Address, Type = item.Type, Label = item.Label });
        }
        if (HasSettings)
            spec.Panels.Add(new PanelInfo { PanelType = "Settings", MachineId = "*", Position = "Separate", Title = "Settings", RequiredLevel = SettingsLevel });
        if (HasPlcDeviceEditor)
            spec.Panels.Add(new PanelInfo { PanelType = "PlcDeviceEditor", MachineId = "*", Position = "DevicePage.Bottom", Title = "PLC Device Editor", RequiredLevel = PlcDeviceEditorLevel });

        return spec;
    }

    // ── Save / Load Spec ─────────────────────────────────────────────────

    private SpecDto BuildSpecDto() => new SpecDto
    {
        ProjectName      = ProjectName,
        HeaderDeviceName = HeaderDeviceName,
        Version          = Version,
        PageMode               = PageMode,
        LayoutMode             = LayoutMode,
        AutoConnect            = AutoConnect,
        RightColumnWidthStar   = RightColumnWidthStar,
        LeftCommandWidthPx     = LeftCommandWidthPx,
        LiveDataTitle          = LiveDataTitle,
        DeviceStatusTitle      = DeviceStatusTitle,
        Machines = Machines.Select(m => new MachineDto
        {
            MachineId    = m.MachineId,
            MachineName  = m.MachineName,
            PlcIp        = m.PlcIp,
            PlcPort      = m.PlcPort,
            PollMs       = m.PollMs,
            IsRunning    = m.IsRunning,
            IsCompleted  = m.IsCompleted,
            IsAlarm      = m.IsAlarm,
            Modules      = m.ModulesString,
            ShowLiveLog  = m.ShowLiveLog,
            MachineDesignFile = m.MachineDesignFile,
            Commands     = m.Commands.Select(c => new CommandDto { Name = c.Name, Address = c.Address, Theme = c.Theme }).ToList(),
            Labels       = m.Labels.Select(l => new LabelDto { Name = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme }).ToList(),
            StatusLabels = m.StatusLabels.Select(l => new LabelDto { Name = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme }).ToList(),
        }).ToList(),
        HasMaintenanceMode    = HasMaintenanceMode,
        HasSettings           = HasSettings,
        HasPlcDeviceEditor    = HasPlcDeviceEditor,
        MaintenanceLevel      = MaintenanceLevel,
        SettingsLevel         = SettingsLevel,
        PlcDeviceEditorLevel  = PlcDeviceEditorLevel,
        MaintenanceItems      = MaintenanceItems.Select(i => new MaintenanceItemDto { MachineId = i.MachineId, ItemName = i.ItemName, Address = i.Address, Type = i.Type, Label = i.Label }).ToList(),
        DataEvents = Machines.SelectMany(m => m.DataEvents.Select(de => new DataEventDto { MachineId = m.MachineId, Name = de.Name, Address = de.Address, Trigger = de.Trigger, Threshold = de.Threshold, DataType = de.DataType })).ToList(),
    };

    private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

    private void SaveSpec()
    {
        var dlg = new SaveFileDialog
        {
            Title = "儲存 Spec",
            Filter = "JSON Spec (*.spec.json)|*.spec.json",
            FileName = ProjectName + ".spec.json",
        };
        if (dlg.ShowDialog() != true) return;

        var json = JsonSerializer.Serialize(BuildSpecDto(), _jsonOpts);
        File.WriteAllText(dlg.FileName, json, System.Text.Encoding.UTF8);
        MessageBox.Show($"已儲存至：{dlg.FileName}", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadSpec()
    {
        var dlg = new OpenFileDialog
        {
            Title = "載入 Spec",
            Filter = "JSON Spec (*.spec.json)|*.spec.json|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var dto = JsonSerializer.Deserialize<SpecDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) return;

            ProjectName      = dto.ProjectName;
            HeaderDeviceName = dto.HeaderDeviceName;
            Version          = dto.Version;
            PageMode               = dto.PageMode;
            LayoutMode             = dto.LayoutMode ?? "SplitRight";
            AutoConnect            = dto.AutoConnect;
            RightColumnWidthStar   = dto.RightColumnWidthStar > 0 ? dto.RightColumnWidthStar : 0.85;
            LeftCommandWidthPx     = dto.LeftCommandWidthPx   > 0 ? dto.LeftCommandWidthPx   : 250;
            LiveDataTitle          = string.IsNullOrEmpty(dto.LiveDataTitle) ? "Live Data" : dto.LiveDataTitle;
            DeviceStatusTitle      = string.IsNullOrEmpty(dto.DeviceStatusTitle) ? "Device Status" : dto.DeviceStatusTitle;

            Machines.Clear();
            foreach (var m in dto.Machines)
            {
                var vm = new MachineViewModel
                {
                    MachineId   = m.MachineId,
                    MachineName = m.MachineName,
                    PlcIp       = m.PlcIp,
                    PlcPort     = m.PlcPort,
                    PollMs      = m.PollMs,
                    IsRunning   = m.IsRunning,
                    IsCompleted = m.IsCompleted,
                    IsAlarm     = m.IsAlarm,
                };
                vm.ShowLiveLog = m.ShowLiveLog;
                vm.MachineDesignFile = m.MachineDesignFile ?? string.Empty;
                vm.ApplyModulesString(m.Modules);
                foreach (var c in m.Commands) vm.Commands.Add(new CommandRow { Name = c.Name, Address = c.Address, Theme = c.Theme });
                foreach (var l in m.Labels)        vm.Labels.Add(new LabelRow { Name = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme });
                foreach (var l in m.StatusLabels)  vm.StatusLabels.Add(new LabelRow { Name = l.Name, Address = l.Address, FrameShape = l.FrameShape, ValueColorTheme = l.ValueColorTheme });
                Machines.Add(vm);

            }
            SelectedMachine = Machines.FirstOrDefault();

            foreach (var de in dto.DataEvents)
            {
                var targetMachine = Machines.FirstOrDefault(m => m.MachineId == de.MachineId);
                targetMachine?.DataEvents.Add(new DataEventRow { Name = de.Name, Address = de.Address, Trigger = de.Trigger, Threshold = de.Threshold, DataType = de.DataType });
            }

            HasMaintenanceMode    = dto.HasMaintenanceMode;
            HasSettings           = dto.HasSettings;
            HasPlcDeviceEditor    = dto.HasPlcDeviceEditor;
            MaintenanceLevel      = dto.MaintenanceLevel;
            SettingsLevel         = dto.SettingsLevel;
            PlcDeviceEditorLevel  = dto.PlcDeviceEditorLevel;

            MaintenanceItems.Clear();
            foreach (var i in dto.MaintenanceItems)
                MaintenanceItems.Add(new MaintenanceItemRow { MachineId = i.MachineId, ItemName = i.ItemName, Address = i.Address, Type = i.Type, Label = i.Label });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));

    // ── DTO models for JSON serialization ────────────────────────────────
    private sealed class SpecDto
    {
        public string ProjectName            { get; set; } = string.Empty;
        public string HeaderDeviceName       { get; set; } = string.Empty;
        public string Version                { get; set; } = "v1.0.0";
        public string PageMode               { get; set; } = "DynamicDevicePage";
        public string LayoutMode             { get; set; } = "SplitRight";
        public bool   AutoConnect            { get; set; }
        public double RightColumnWidthStar   { get; set; } = 0.85;
        public int    LeftCommandWidthPx     { get; set; } = 250;
        public string LiveDataTitle          { get; set; } = "Live Data";
        public string DeviceStatusTitle      { get; set; } = "Device Status";
        public List<MachineDto> Machines { get; set; } = [];
        public bool HasMaintenanceMode    { get; set; }
        public bool HasSettings           { get; set; }
        public bool HasPlcDeviceEditor    { get; set; }
        public string MaintenanceLevel    { get; set; } = "Supervisor";
        public string SettingsLevel       { get; set; } = "Admin";
        public string PlcDeviceEditorLevel { get; set; } = "Supervisor";
        public List<MaintenanceItemDto> MaintenanceItems { get; set; } = [];
        public List<DataEventDto> DataEvents { get; set; } = [];
    }
    private sealed class DataEventDto
    {
        public string MachineId  { get; set; } = string.Empty;
        public string Name       { get; set; } = string.Empty;
        public string Address    { get; set; } = string.Empty;
        public string Trigger    { get; set; } = "changed";
        public int    Threshold  { get; set; } = 0;
        public string DataType   { get; set; } = string.Empty;
    }
    private sealed class MachineDto
    {
        public string MachineId   { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string PlcIp       { get; set; } = "192.168.1.100";
        public int    PlcPort     { get; set; } = 3000;
        public int    PollMs      { get; set; } = 200;
        public string IsRunning   { get; set; } = "M200";
        public string IsCompleted { get; set; } = "M202";
        public string IsAlarm     { get; set; } = "M201";
        public string Modules     { get; set; } = "processControl";
        public bool   ShowLiveLog { get; set; } = false;
        public string MachineDesignFile { get; set; } = string.Empty;
        public List<CommandDto> Commands     { get; set; } = [];
        public List<LabelDto>   Labels       { get; set; } = [];
        public List<LabelDto>   StatusLabels { get; set; } = [];
    }
    private sealed class CommandDto { public string Name { get; set; } = string.Empty; public string Address { get; set; } = string.Empty; public string Theme { get; set; } = string.Empty; }
    private sealed class LabelDto   { public string Name { get; set; } = string.Empty; public string Address { get; set; } = string.Empty; public string FrameShape { get; set; } = "Rectangle"; public string ValueColorTheme { get; set; } = "NeonBlue"; }
    private sealed class MaintenanceItemDto
    {
        public string MachineId { get; set; } = "*";
        public string ItemName  { get; set; } = string.Empty;
        public string Address   { get; set; } = string.Empty;
        public string Type      { get; set; } = "toggle";
        public string Label     { get; set; } = string.Empty;
    }
}

public sealed class MaintenanceItemRow : INotifyPropertyChanged
{
    private string _machineId = "*";
    private string _itemName  = string.Empty;
    private string _address   = string.Empty;
    private string _type      = "toggle";
    private string _label     = string.Empty;

    public string MachineId { get => _machineId; set { _machineId = value; N(); } }
    public string ItemName  { get => _itemName;  set { _itemName  = value; N(); } }
    public string Address   { get => _address;   set { _address   = value; N(); } }
    public string Type      { get => _type;      set { _type      = value; N(); } }
    public string Label     { get => _label;     set { _label     = value; N(); } }

    public string[] Types { get; } = ["toggle", "momentary", "editor", "readonly"];

    public event PropertyChangedEventHandler? PropertyChanged;
    private void N([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new(n));
}

// ── Simple RelayCommand ───────────────────────────────────────────────────

internal sealed class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? p) => canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => execute(p);
}

internal sealed class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? p) => canExecute?.Invoke(p is T t ? t : default) ?? true;
    public void Execute(object? p) => execute(p is T t ? t : default);
}
