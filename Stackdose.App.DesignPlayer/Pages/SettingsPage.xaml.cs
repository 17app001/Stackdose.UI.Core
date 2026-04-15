using Microsoft.Win32;
using Stackdose.App.DesignPlayer.Models;
using Stackdose.App.DesignPlayer.Services;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.DesignPlayer.Pages;

/// <summary>
/// 讓操作員修改 PLC 連線參數與設計稿路徑，無需手動編輯 JSON。
/// 按「套用並儲存」後寫回 app-config.json，下次啟動時生效；
/// 若需立即重連，由 ApplyRequested 事件通知 MainWindow。
/// </summary>
public partial class SettingsPage : UserControl
{
    private readonly string _configPath;
    private PlayerAppConfig _config;

    /// <summary>
    /// 使用者按下「套用並儲存」後觸發；MainWindow 可據此重載連線。
    /// </summary>
    public event EventHandler<PlayerAppConfig>? ApplyRequested;

    public SettingsPage(string configPath, PlayerAppConfig config)
    {
        _configPath = configPath;
        _config     = config;
        InitializeComponent();
        LoadFields();
    }

    private void LoadFields()
    {
        txtPlcIp.Text       = _config.Plc.Ip;
        txtPlcPort.Text     = _config.Plc.Port.ToString();
        txtPollInterval.Text = _config.Plc.PollIntervalMs.ToString();
        chkAutoConnect.IsChecked = _config.Plc.AutoConnect;
        txtDesignFile.Text  = _config.DesignFile;
        txtAppTitle.Text    = _config.AppTitle;
        txtDeviceName.Text  = _config.HeaderDeviceName;
        lblStatus.Text      = "";
    }

    private void OnBrowseDesignFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "選擇設計稿",
            Filter = "Machine Design|*.machinedesign.json|All Files|*.*",
        };
        if (dlg.ShowDialog() == true)
            txtDesignFile.Text = dlg.FileName;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtPlcPort.Text, out var port) || port is < 1 or > 65535)
        {
            lblStatus.Text = "埠號格式錯誤（1–65535）";
            return;
        }
        if (!int.TryParse(txtPollInterval.Text, out var poll) || poll < 50)
        {
            lblStatus.Text = "輪詢間隔最小 50 ms";
            return;
        }
        if (string.IsNullOrWhiteSpace(txtDesignFile.Text))
        {
            lblStatus.Text = "請指定設計稿路徑";
            return;
        }

        _config.Plc.Ip            = txtPlcIp.Text.Trim();
        _config.Plc.Port          = port;
        _config.Plc.PollIntervalMs = poll;
        _config.Plc.AutoConnect   = chkAutoConnect.IsChecked == true;
        _config.DesignFile        = txtDesignFile.Text.Trim();
        _config.AppTitle          = txtAppTitle.Text.Trim();
        _config.HeaderDeviceName  = txtDeviceName.Text.Trim();

        try
        {
            PlayerConfigLoader.Save(_configPath, _config);
            lblStatus.Text = "已儲存（下次啟動時生效）";
            ApplyRequested?.Invoke(this, _config);
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"儲存失敗：{ex.Message}";
        }
    }
}
