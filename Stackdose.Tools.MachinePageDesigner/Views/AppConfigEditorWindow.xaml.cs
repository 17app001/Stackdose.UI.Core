using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

/// <summary>
/// app-config.json 視覺化編輯器。
/// 傳入目前開啟的 .machinedesign.json 路徑，
/// 自動定位同目錄的 app-config.json 進行讀寫。
/// </summary>
public partial class AppConfigEditorWindow : Window
{
    private readonly string _configPath;

    private static readonly JsonSerializerOptions s_readOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly JsonSerializerOptions s_writeOpts = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// <paramref name="designFilePath"/>: 當前 .machinedesign.json 的完整路徑。
    /// app-config.json 存放在同一目錄。
    /// </summary>
    public AppConfigEditorWindow(string designFilePath)
    {
        InitializeComponent();

        var dir = Path.GetDirectoryName(designFilePath) ?? AppContext.BaseDirectory;
        _configPath = Path.Combine(dir, "app-config.json");

        // 顯示儲存路徑
        SavePathText.Text = $"存檔位置：{_configPath}";

        // 計算 designFile 相對路徑（慣例 Config/{filename}）
        var autoDesignFile = "Config/" + Path.GetFileName(designFilePath);

        // 嘗試讀取現有 app-config.json
        var draft = TryLoad(_configPath) ?? new AppConfigDraft
        {
            DesignFile = autoDesignFile,
        };

        // 若 designFile 為空，填入自動計算的值
        if (string.IsNullOrWhiteSpace(draft.DesignFile))
            draft.DesignFile = autoDesignFile;

        PopulateForm(draft);
    }

    // ── Form 填充 / 讀取 ─────────────────────────────────────────

    private void PopulateForm(AppConfigDraft d)
    {
        AppTitleBox.Text      = d.AppTitle;
        DeviceNameBox.Text    = d.HeaderDeviceName;
        LoginRequiredBox.IsChecked = d.LoginRequired;

        PlcIpBox.Text         = d.PlcIp;
        PlcPortBox.Text       = d.PlcPort.ToString();
        PollIntervalBox.Text  = d.PollIntervalMs.ToString();
        AutoConnectBox.IsChecked = d.AutoConnect;

        DesignFileBox.Text    = d.DesignFile;
    }

    private AppConfigDraft ReadForm()
    {
        int.TryParse(PlcPortBox.Text,        out var port);
        int.TryParse(PollIntervalBox.Text,   out var poll);

        return new AppConfigDraft
        {
            AppTitle         = AppTitleBox.Text.Trim(),
            HeaderDeviceName = DeviceNameBox.Text.Trim(),
            LoginRequired    = LoginRequiredBox.IsChecked == true,
            PlcIp            = PlcIpBox.Text.Trim(),
            PlcPort          = port > 0 ? port : 3000,
            PollIntervalMs   = poll > 0 ? poll : 500,
            AutoConnect      = AutoConnectBox.IsChecked == true,
            DesignFile       = DesignFileBox.Text.Trim(),
        };
    }

    // ── 按鈕事件 ──────────────────────────────────────────────────

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var draft = ReadForm();

        if (string.IsNullOrWhiteSpace(draft.AppTitle))
        { MessageBox.Show("App 名稱不能為空。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        if (string.IsNullOrWhiteSpace(draft.PlcIp))
        { MessageBox.Show("PLC IP 位址不能為空。", "驗證失敗", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        try
        {
            // 序列化成 PlayerAppConfig 格式（camelCase）
            var payload = new
            {
                appTitle         = draft.AppTitle,
                headerDeviceName = draft.HeaderDeviceName,
                loginRequired    = draft.LoginRequired,
                plc = new
                {
                    ip            = draft.PlcIp,
                    port          = draft.PlcPort,
                    pollIntervalMs = draft.PollIntervalMs,
                    autoConnect   = draft.AutoConnect,
                },
                designFile = draft.DesignFile,
            };

            var json = JsonSerializer.Serialize(payload, s_writeOpts);
            File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);

            MessageBox.Show($"已儲存至：\n{_configPath}", "儲存成功",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    // ── 讀取現有 JSON ─────────────────────────────────────────────

    private static AppConfigDraft? TryLoad(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            // 讀取原始 JSON，手動對應到 AppConfigDraft
            using var doc = JsonDocument.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            var root = doc.RootElement;

            var draft = new AppConfigDraft();
            if (root.TryGetProperty("appTitle",         out var v)) draft.AppTitle         = v.GetString() ?? draft.AppTitle;
            if (root.TryGetProperty("headerDeviceName", out v))     draft.HeaderDeviceName = v.GetString() ?? draft.HeaderDeviceName;
            if (root.TryGetProperty("loginRequired",    out v))     draft.LoginRequired    = v.GetBoolean();
            if (root.TryGetProperty("designFile",       out v))     draft.DesignFile       = v.GetString() ?? draft.DesignFile;

            if (root.TryGetProperty("plc", out var plc))
            {
                if (plc.TryGetProperty("ip",             out v)) draft.PlcIp          = v.GetString() ?? draft.PlcIp;
                if (plc.TryGetProperty("port",           out v)) draft.PlcPort        = v.GetInt32();
                if (plc.TryGetProperty("pollIntervalMs", out v)) draft.PollIntervalMs = v.GetInt32();
                if (plc.TryGetProperty("autoConnect",    out v)) draft.AutoConnect    = v.GetBoolean();
            }
            return draft;
        }
        catch { return null; }
    }
}
