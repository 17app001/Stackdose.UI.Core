using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Controls;
using Stackdose.Abstractions.Hardware;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// Monitors PLC addresses and fires events when configured trigger conditions are met.
/// First scan records initial values only — no events fired.
/// Subsequent scans compare previous vs current and fire only on actual changes.
/// </summary>
public sealed class PlcDataEventMonitor
{
    // State tracked per event entry: previous value (null = first scan)
    private sealed record EntryState(DataEventConfig Config, int? PreviousValue);

    private PlcStatus? _subscribedStatus;
    private List<EntryState> _entries = [];

    /// <summary>
    /// Fired when a data event trigger condition is met.
    /// Parameters: (eventName, address, oldVal, newVal)
    /// </summary>
    public Action<string, string, int, int>? EventTriggered { get; set; }

    public void Subscribe(PlcStatus status, IReadOnlyList<DataEventConfig> configs)
    {
        if (_subscribedStatus != null)
            _subscribedStatus.ScanUpdated -= OnScanUpdated;

        _subscribedStatus = status;
        _entries = configs.Select(c => new EntryState(c, null)).ToList();

        _subscribedStatus.ScanUpdated += OnScanUpdated;
    }

    public void Unsubscribe()
    {
        if (_subscribedStatus != null)
        {
            _subscribedStatus.ScanUpdated -= OnScanUpdated;
            _subscribedStatus = null;
        }
        _entries = [];
    }

    private void OnScanUpdated(IPlcManager manager)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var cfg = entry.Config;

            var currentValue = ReadValue(manager, cfg);
            if (currentValue is null)
                continue; // skip if read failed

            var prev = entry.PreviousValue;
            _entries[i] = entry with { PreviousValue = currentValue.Value };

            int newVal = currentValue.Value;

            if (prev is null)
            {
                // 第一次掃描：直接評估當前值是否已滿足條件
                // "changed" 除外（沒有前值，無法判斷變動）
                bool firstScanFire = cfg.Trigger.ToLowerInvariant() switch
                {
                    "risingedge"  => newVal != 0,
                    "fallingedge" => newVal == 0,
                    "above"       => newVal > cfg.Threshold,
                    "below"       => newVal < cfg.Threshold,
                    "equals"      => newVal == cfg.Threshold,
                    _             => false,
                };
                if (firstScanFire)
                    EventTriggered?.Invoke(cfg.Name, cfg.Address, 0, newVal);
                continue;
            }

            int oldVal = prev.Value;

            if (oldVal == newVal)
                continue; // value unchanged — never trigger

            bool shouldFire = cfg.Trigger.ToLowerInvariant() switch
            {
                "risingedge"  => oldVal == 0 && newVal != 0,
                "fallingedge" => oldVal != 0 && newVal == 0,
                "above"       => oldVal <= cfg.Threshold && newVal > cfg.Threshold,
                "below"       => oldVal >= cfg.Threshold && newVal < cfg.Threshold,
                "equals"      => newVal == cfg.Threshold,
                _             => true,   // "changed" and any unknown: fire on any change
            };

            if (shouldFire)
                EventTriggered?.Invoke(cfg.Name, cfg.Address, oldVal, newVal);
        }
    }

    private static int? ReadValue(IPlcManager manager, DataEventConfig cfg)
    {
        bool isBit = IsBitAddress(cfg);
        if (isBit)
        {
            bool? v = manager.ReadBit(cfg.Address);
            if (v is null) return null;
            return v.Value ? 1 : 0;
        }
        else
        {
            short? v = manager.ReadWord(cfg.Address);
            if (v is null) return null;
            return v.Value;
        }
    }

    private static bool IsBitAddress(DataEventConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.DataType))
            return cfg.DataType.Equals("bit", StringComparison.OrdinalIgnoreCase);

        // Auto-detect from address prefix
        if (string.IsNullOrWhiteSpace(cfg.Address)) return false;
        char prefix = char.ToUpperInvariant(cfg.Address.TrimStart()[0]);
        return prefix == 'M' || prefix == 'X' || prefix == 'Y';
    }
}
