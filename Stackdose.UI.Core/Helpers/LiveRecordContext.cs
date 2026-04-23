using Stackdose.UI.Core.Controls;
using System.Windows;

namespace Stackdose.UI.Core.Helpers;

/// <summary>
/// 全局即時數據記錄管理器。
/// 單一 Timer 定期掃描所有已註冊 PlcLabel，將啟用記錄且有效值寫入 SQLite。
/// </summary>
public static class LiveRecordContext
{
    private static System.Threading.Timer? _timer;
    private static readonly List<WeakReference<PlcLabel>> _labels = new();
    private static readonly object _lock = new();
    private static int _intervalSec = 5;

    /// <param name="intervalSec">記錄間隔秒數。0 表示停用即時紀錄。</param>
    public static void Start(int intervalSec = 5)
    {
        _timer?.Dispose();
        _timer = null;
        if (intervalSec <= 0) return;

        _intervalSec = intervalSec;
        int ms = intervalSec * 1000;
        _timer = new System.Threading.Timer(_ => OnTick(), null, ms, ms);
    }

    public static void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public static void Register(PlcLabel label)
    {
        lock (_lock)
        {
            _labels.Add(new WeakReference<PlcLabel>(label));
        }
    }

    public static void Unregister(PlcLabel label)
    {
        lock (_lock)
        {
            _labels.RemoveAll(wr => !wr.TryGetTarget(out var t) || ReferenceEquals(t, label));
        }
    }

    private static void OnTick()
    {
        if (Application.Current?.Dispatcher is not { HasShutdownStarted: false } dispatcher) return;

        List<WeakReference<PlcLabel>> snapshot;
        lock (_lock)
        {
            snapshot = _labels.ToList();
            _labels.RemoveAll(wr => !wr.TryGetTarget(out _));
        }

        int intervalSec = _intervalSec;
        dispatcher.BeginInvoke(() =>
        {
            if (dispatcher.HasShutdownStarted) return;
            foreach (var wr in snapshot)
            {
                if (!wr.TryGetTarget(out var label)) continue;
                if (!label.EnableLiveRecord) continue;
                var value = label.Value;
                if (string.IsNullOrEmpty(value) || value == "-") continue;
                SqliteLogger.EnqueueLiveRecord(label.Address, label.Label, value, intervalSec);
            }
        });
    }
}
