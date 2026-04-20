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

            var args = $"publish \"{projPath}\" -c Release -r win-x64 --self-contained false"
                     + $" /p:AssemblyName=\"{exeName}\" -o \"{outputDir}\"";

            var success = await RunDotnetAsync(args);

            if (success)
            {
                // Copy design file
                var configDir = Path.Combine(outputDir, "Config");
                Directory.CreateDirectory(configDir);
                var destDesign = Path.Combine(configDir, Path.GetFileName(_designFilePath));
                File.Copy(_designFilePath, destDesign, overwrite: true);
                Log($"[複製] 設計稿 → Config/{Path.GetFileName(_designFilePath)}");

                // Copy app-config.json
                File.Copy(configPath, Path.Combine(outputDir, "app-config.json"), overwrite: true);
                Log("[複製] app-config.json");

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

    private async Task<bool> RunDotnetAsync(string args)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
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
