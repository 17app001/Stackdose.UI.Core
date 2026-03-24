using Stackdose.App.DeviceFramework.Models;
using Stackdose.App.DeviceFramework.Services;

namespace Stackdose.App.UbiDemo.Services;

/// <summary>
/// Ubi 專用的 RuntimeMapper，在 CreateDeviceContext 中
/// 額外將 Tag 地址（batchNo, recipeNo, nozzleTemp）加入 Labels，
/// 讓 UbiDeviceContextMapper 可以正確轉換。
/// </summary>
internal sealed class UbiRuntimeMapper : RuntimeMapper
{
    public UbiRuntimeMapper(IRuntimeMappingAdapter adapter) : base(adapter) { }

    public override DeviceContext CreateDeviceContext(MachineConfig config)
    {
        var context = base.CreateDeviceContext(config);

        // 將 Ubi 需要的 Tag 地址加入 Labels（如果尚未存在）
        AddTagToLabels(context, config, "process", "batchNo");
        AddTagToLabels(context, config, "process", "recipeNo");
        AddTagToLabels(context, config, "process", "nozzleTemp");

        // 將 DetailLabels 的 fallback 也加入（如果 config 中沒有的話）
        EnsureLabel(context, "totalTray", config, "D3400");
        EnsureLabel(context, "currentTray", config, "D33");
        EnsureLabel(context, "totalLayer", config, "D3401");
        EnsureLabel(context, "currentLayer", config, "D32");
        EnsureLabel(context, "switchGraphicLayer", config, "D510");
        EnsureLabel(context, "switchAreaLayer", config, "D512");
        EnsureLabel(context, "messageId", config, "D85");
        EnsureLabel(context, "battery", config, "D120");
        EnsureLabel(context, "elapsedTime", config, "D86");
        EnsureLabel(context, "printHeadCount", config, "D87");

        return context;
    }

    private void AddTagToLabels(DeviceContext context, MachineConfig config, string section, string key)
    {
        if (context.Labels.ContainsKey(key))
            return;

        var address = MappingAdapter.GetTagAddress(config, section, key);
        if (!string.IsNullOrWhiteSpace(address) && address != "--")
        {
            context.Labels[key] = new DeviceLabelInfo(address);
        }
    }

    private void EnsureLabel(DeviceContext context, string key, MachineConfig config, string fallback)
    {
        if (context.Labels.ContainsKey(key))
            return;

        var address = MappingAdapter.GetDetailLabelAddress(config, key, fallback);
        if (!string.IsNullOrWhiteSpace(address))
        {
            context.Labels[key] = new DeviceLabelInfo(address);
        }
    }
}
