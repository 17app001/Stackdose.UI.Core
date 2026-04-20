using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
// Note: no System.Windows.Forms — uses OpenFileDialog folder-select hack instead

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class PublishDashboardWindow : Window
{
    private static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Stackdose", "Designer", "publish-settings.json");

    private readonly string _designFilePath;
    private string? _lastOutputDir;

    public PublishDashboardWindow(string designFilePath, string machineId)
    {
        InitializeComponent();
        _designFilePath = designFilePath;

        ExeNameBox.Text = machineId;
        OutputDirBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"{machineId}-app");

        LoadSettings();

        var found = FindDesignPlayerCsproj();
        if (found != null) ProjPathBox.Text = found;

        Log("就緒。填寫設定後點「封裝」開始編譯。");
    }

    // ── Browse buttons ────────────────────────────────────────────────

    private void OnBrowseOutput(object sender, RoutedEventArgs e)
    {
        // WPF folder-select trick: OpenFileDialog with ValidateNames=false
        var dlg = new OpenFileDialog
        {
            Title            = "選擇輸出資料夾（在目標資料夾內點「開啟」）",
            FileName         = "選擇此資料夾",
            CheckFileExists  = false,
            CheckPathExists  = true,
            ValidateNames    = false,
            InitialDirectory = Directory.Exists(OutputDirBox.Text)
                               ? OutputDirBox.Text
                               : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
        if (dlg.ShowDialog() == true)
            OutputDirBox.Text = Path.GetDirectoryName(dlg.FileName)!;
    }

    private void OnBrowseAppConfig(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "選擇 app-config.json",
            Filter = "JSON 設定|app-config.json|所有 JSON|*.json",
            InitialDirectory = string.IsNullOrEmpty(AppConfigPathBox.Text)
                ? Path.GetDirectoryName(_designFilePath)
                : Path.GetDirectoryName(AppConfigPathBox.Text)
        };
        if (dlg.ShowDialog() == true)
            AppConfigPathBox.Text = dlg.FileName;
    }

    private void OnBrowseProj(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "選擇 DesignPlayer.csproj",
            Filter = "C# 專案|*.csproj",
            InitialDirectory = string.IsNullOrEmpty(ProjPathBox.Text)
                ? AppDomain.CurrentDomain.BaseDirectory
                : Path.GetDirectoryName(ProjPathBox.Text)
        };
        if (dlg.ShowDialog() == true)
            ProjPathBox.Text = dlg.FileName;
    }

    // ── Publish ───────────────────────────────────────────────────────

    private async void OnPublish(object sender, RoutedEventArgs e)
    {
        var exeName    = ExeNameBox.Text.Trim();
        var outputDir  = OutputDirBox.Text.Trim();
        var configPath = AppConfigPathBox.Text.Trim();
        var projPath   = ProjPathBox.Text.Trim();

        if (!Validate(exeName, outputDir, configPath, projPath)) return;

        PublishBtn.IsEnabled = false;
        CloseBtn.IsEnabled   = false;
        OpenFolderBtn.Visibility = Visibility.Collapsed;
        _lastOutputDir = outputDir;

        Log("");
        Log($"[封裝開始] {DateTime.Now:HH:mm:ss}");
        Log($"  exe 名稱：{exeName}");
        Log($"  輸出目錄：{outputDir}");
        Log($"  專案：{projPath}");
        Log("────────────────────────────────");

        try
        {
            Directory.CreateDirectory(outputDir);

            // Note: /p:AssemblyName causes NuGet "Ambiguous project name" error.
            // Publish with original name, then rename output files afterward.
            // DebugType=None suppresses .pdb generation at the source.
            var args = $"publish \"{projPath}\" -c Release -r win-x64 --self-contained true"
                     + $" /p:DebugType=None /p:DebugSymbols=false /p:PublishReadyToRun=false"
                     + $" -o \"{outputDir}\"";

            var exitedClean = await RunDotnetAsync(args);

            // FeiyangWrapper.vcxproj (C++ native) can't be built by dotnet CLI,
            // so exit code may be non-zero even though all C# output was produced.
            // Treat as success if the published exe actually exists.
            var publishedExe = Path.Combine(outputDir, "Stackdose.App.DesignPlayer.exe");
            var success = exitedClean || File.Exists(publishedExe);

            if (!exitedClean && success)
                Log("[提示] 偵測到 C++ 原生元件建置警告（FeiyangWrapper），但 C# 輸出完整，繼續封裝。");

            if (success)
            {
                // Rename exe + companion files to desired exeName
                RenamePublishOutput(outputDir, exeName);

                // Allow running on .NET 9/10+ without requiring exact .NET 8 installation
                PatchRollForward(outputDir, exeName);

                // Remove dev-only and unused-module files
                CleanPublishOutput(outputDir, exeName);

                // Copy design file
                var configDir = Path.Combine(outputDir, "Config");
                Directory.CreateDirectory(configDir);
                var destDesign = Path.Combine(configDir, Path.GetFileName(_designFilePath));
                File.Copy(_designFilePath, destDesign, overwrite: true);
                Log($"[複製] 設計稿 → Config/{Path.GetFileName(_designFilePath)}");

                // Copy app-config.json into Config/ (App reads Config/app-config.json)
                File.Copy(configPath, Path.Combine(configDir, "app-config.json"), overwrite: true);
                Log("[複製] app-config.json → Config/app-config.json");

                Log("────────────────────────────────");
                Log($"[完成] ✅ 封裝成功！{DateTime.Now:HH:mm:ss}");
                Log($"  執行：{outputDir}\\{exeName}.exe");

                OpenFolderBtn.Visibility = Visibility.Visible;
                SaveSettings();
            }
            else
            {
                Log("────────────────────────────────");
                Log("[失敗] ❌ dotnet publish 回傳錯誤，請查看上方輸出。");
            }
        }
        catch (Exception ex)
        {
            Log($"[例外] {ex.Message}");
        }
        finally
        {
            PublishBtn.IsEnabled = true;
            CloseBtn.IsEnabled   = true;
        }
    }

    private void CleanPublishOutput(string outputDir, string exeName)
    {
        // MachinePageDesigner is a design tool pulled in as ProjectReference for model types.
        // DesignPlayer only needs the .dll; the standalone launcher files are not required.
        var designerBase = Path.Combine(outputDir, "Stackdose.Tools.MachinePageDesigner");
        foreach (var ext in new[] { ".exe", ".runtimeconfig.json", ".deps.json", ".pdb" })
            TryDeleteFile(designerBase + ext);

        // PrintHead / FeiyangWrapper — C++ native DLL for ink-jet head, not used in Dashboard
        foreach (var prefix in new[] { "FeiyangWrapper", "Stackdose.PrintHead" })
        {
            foreach (var f in Directory.GetFiles(outputDir, prefix + ".*"))
                TryDeleteFile(f);
        }

        // Sweep any remaining .pdb (DebugType=None should have suppressed these,
        // but some platform DLLs may still ship them)
        foreach (var pdb in Directory.GetFiles(outputDir, "*.pdb"))
            TryDeleteFile(pdb);

        Log("[清理] 移除 .pdb、MachinePageDesigner 執行檔、FeiyangWrapper、PrintHead");
    }

    private void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception ex) { Log($"[清理跳過] {Path.GetFileName(path)}: {ex.Message}"); }
    }

    private static void PatchRollForward(string outputDir, string exeName)
    {
        var path = Path.Combine(outputDir, exeName + ".runtimeconfig.json");
        if (!File.Exists(path)) return;

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        using var stream = new System.IO.MemoryStream();
        using var writer = new System.Text.Json.Utf8JsonWriter(stream, new System.Text.Json.JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WritePropertyName("runtimeOptions");
        writer.WriteStartObject();
        writer.WriteString("rollForward", "LatestMajor");
        foreach (var prop in root.GetProperty("runtimeOptions").EnumerateObject())
            prop.WriteTo(writer);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();

        File.WriteAllBytes(path, stream.ToArray());
    }

    private static void RenamePublishOutput(string outputDir, string exeName)
    {
        const string srcName = "Stackdose.App.DesignPlayer";
        foreach (var ext in new[] { ".exe", ".runtimeconfig.json", ".deps.json" })
        {
            var src = Path.Combine(outputDir, srcName + ext);
            var dst = Path.Combine(outputDir, exeName + ext);
            if (File.Exists(src) && src != dst)
                File.Move(src, dst, overwrite: true);
        }
    }

    private async Task<bool> RunDotnetAsync(string args)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            UseShellExecute         = false,
            RedirectStandardOutput  = true,
            RedirectStandardError   = true,
            CreateNoWindow          = true,
            StandardOutputEncoding  = System.Text.Encoding.UTF8,
            StandardErrorEncoding   = System.Text.Encoding.UTF8,
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) Dispatcher.InvokeAsync(() => Log(e.Data)); };
        proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) Dispatcher.InvokeAsync(() => Log($"[stderr] {e.Data}")); };

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);
        return proc.ExitCode == 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private bool Validate(string exeName, string outputDir, string configPath, string projPath)
    {
        if (string.IsNullOrEmpty(exeName))
        { MessageBox.Show("請填寫執行檔名稱。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }

        if (string.IsNullOrEmpty(outputDir))
        { MessageBox.Show("請填寫輸出資料夾。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }

        if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
        { MessageBox.Show("app-config.json 路徑無效，請重新選擇。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }

        if (string.IsNullOrEmpty(projPath) || !File.Exists(projPath))
        { MessageBox.Show("找不到 DesignPlayer.csproj，請手動選擇路徑。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }

        return true;
    }

    private void Log(string message)
    {
        LogBox.AppendText(message + "\n");
        LogScroll.ScrollToEnd();
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastOutputDir) && Directory.Exists(_lastOutputDir))
            Process.Start("explorer.exe", _lastOutputDir);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    // ── Auto-detect DesignPlayer.csproj ──────────────────────────────

    private static string? FindDesignPlayerCsproj()
    {
        const string csprojName = "Stackdose.App.DesignPlayer.csproj";
        const string folderName = "Stackdose.App.DesignPlayer";

        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, folderName, csprojName);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    // ── Settings persistence ──────────────────────────────────────────

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var doc  = JsonDocument.Parse(json).RootElement;

            if (doc.TryGetProperty("appConfigPath", out var v) && v.ValueKind == JsonValueKind.String)
                AppConfigPathBox.Text = v.GetString() ?? "";
            if (doc.TryGetProperty("projPath", out var p) && p.ValueKind == JsonValueKind.String)
                ProjPathBox.Text = p.GetString() ?? "";
        }
        catch { /* ignore stale settings */ }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var obj = new { appConfigPath = AppConfigPathBox.Text, projPath = ProjPathBox.Text };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(obj,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best-effort */ }
    }
}
