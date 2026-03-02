using Stackdose.App.SingleDetailLab.Models;
using Stackdose.App.SingleDetailLab.Services;
using System.Windows.Controls;

namespace Stackdose.App.SingleDetailLab.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(LabMachineConfig config)
    {
        MachineSummaryText.Text = $"Machine: {config.Machine.Name} ({config.Machine.Id})";
        TopPlcStatus.IpAddress = config.Plc.Ip;
        TopPlcStatus.Port = config.Plc.Port;
        TopPlcStatus.ScanInterval = config.Plc.PollIntervalMs;
        TopPlcStatus.AutoConnect = config.Plc.AutoConnect;
        TopPlcStatus.MonitorAddress = LabMonitorAddressBuilder.Build(config);

        DefaultBatchLabel.Address = GetTagAddress(config, "process", "batchNo", "D400");
        DefaultRecipeLabel.Address = GetTagAddress(config, "process", "recipeNo", "D410");
        DefaultEditor.Address = GetTagAddress(config, "process", "nozzleTemp", "D420");
        DefaultEditor.Value = "0";
    }

    private static string GetTagAddress(LabMachineConfig config, string section, string key, string fallback)
    {
        Dictionary<string, LabTagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status" => config.Tags.Status,
            "process" => config.Tags.Process,
            _ => null
        };

        if (tags is null || !tags.TryGetValue(key, out var tag) || string.IsNullOrWhiteSpace(tag.Address))
        {
            return fallback;
        }

        return tag.Address;
    }
}
