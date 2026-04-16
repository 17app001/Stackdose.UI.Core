using Stackdose.Abstractions.Hardware;
using Stackdose.App.DeviceFramework.Models;
using Stackdose.UI.Core.Helpers;
using System.Text.RegularExpressions;

namespace Stackdose.App.DeviceFramework.Services;

/// <summary>
/// 序列指令執行器：逐步執行 CommandSequenceDefinition 中的步驟
/// </summary>
public sealed class SequenceExecutor
{
    private readonly Dictionary<string, int> _variables = new();
    private readonly string _machineId;
    private readonly string _machineName;
    private readonly List<string> _executionLog = [];

    public SequenceExecutor(string machineId, string machineName)
    {
        _machineId = machineId;
        _machineName = machineName;
    }

    /// <summary>目前執行的步驟 ID（供 UI 顯示進度）</summary>
    public string? CurrentStepId { get; private set; }

    /// <summary>執行紀錄</summary>
    public IReadOnlyList<string> ExecutionLog => _executionLog;

    /// <summary>
    /// 執行完整序列
    /// </summary>
    public async Task<SequenceExecutionResult> ExecuteAsync(CommandSequenceDefinition sequence)
    {
        var manager = PlcContext.GlobalStatus?.CurrentManager;
        if (manager is null || !manager.IsConnected)
            return Fail("PLC 未連線，無法執行序列指令");

        _variables.Clear();
        _executionLog.Clear();

        try
        {
            var result = await ExecuteStepsAsync(sequence.Steps, manager);
            if (!result.Success && sequence.OnError == "rollback" && sequence.Rollback?.Count > 0)
            {
                Log("⚠ 執行失敗，開始回滾...");
                await ExecuteStepsAsync(sequence.Rollback, manager);
                return Fail($"序列執行失敗並已回滾：{result.Message}");
            }
            return result;
        }
        catch (Exception ex)
        {
            if (sequence.OnError == "rollback" && sequence.Rollback?.Count > 0)
            {
                Log($"⚠ 例外: {ex.Message}，開始回滾...");
                try { await ExecuteStepsAsync(sequence.Rollback, manager); }
                catch { /* rollback best-effort */ }
            }
            return Fail($"序列執行例外：{ex.Message}");
        }
    }

    private async Task<SequenceExecutionResult> ExecuteStepsAsync(
        List<SequenceStep> steps,
        IPlcManager manager)
    {
        foreach (var step in steps)
        {
            CurrentStepId = step.Id;
            var result = await ExecuteStepAsync(step, manager);
            if (!result.Success)
                return result;
        }
        return Ok("序列執行完成");
    }

    private async Task<SequenceExecutionResult> ExecuteStepAsync(
        SequenceStep step,
        IPlcManager manager)
    {
        switch (step)
        {
            case WriteStep ws:
                return await ExecuteWriteAsync(ws, manager);

            case ReadStep rs:
                return ExecuteRead(rs, manager);

            case WaitStep ws:
                Log($"[wait] 等待 {ws.DelayMs}ms - {ws.Description}");
                await Task.Delay(Math.Max(10, ws.DelayMs));
                return Ok();

            case ConditionalStep cs:
                return await ExecuteConditionalAsync(cs, manager);

            case ReadWaitStep rws:
                return await ExecuteReadWaitAsync(rws, manager);

            default:
                return Fail($"未知的步驟類型：{step.GetType().Name}");
        }
    }

    private async Task<SequenceExecutionResult> ExecuteWriteAsync(WriteStep step, IPlcManager manager)
    {
        var address = step.Address;
        var value = ResolveVariables(step.Value);

        Log($"[write] {address} = {value} - {step.Description}");

        var ok = await manager.WriteAsync($"{address},{value}");
        if (!ok)
            return Fail($"寫入失敗：{address}={value}");

        ComplianceContext.LogSystem(
            $"[Sequence] Write: {_machineName} ({_machineId}) -> {address}={value} | {step.Description}",
            machineId: _machineId);

        return Ok();
    }

    private SequenceExecutionResult ExecuteRead(ReadStep step, IPlcManager manager)
    {
        int? value;
        if (step.DataType == "bit")
        {
            var bit = manager.ReadBit(step.Address);
            value = bit == true ? 1 : 0;
        }
        else
        {
            value = manager.ReadWord(step.Address);
        }

        if (value is null)
            return Fail($"讀取失敗：{step.Address}");

        _variables[step.Variable] = value.Value;
        Log($"[read] {step.Address} -> ${{{step.Variable}}} = {value.Value} - {step.Description}");
        return Ok();
    }

    private async Task<SequenceExecutionResult> ExecuteConditionalAsync(
        ConditionalStep step, IPlcManager manager)
    {
        var condResult = EvaluateCondition(step.Condition);
        Log($"[conditional] {step.Condition} = {condResult} - {step.Description}");

        if (condResult)
        {
            return await ExecuteStepsAsync(step.OnTrue, manager);
        }
        else if (step.OnFalse?.Count > 0)
        {
            return await ExecuteStepsAsync(step.OnFalse, manager);
        }

        return Ok();
    }

    private async Task<SequenceExecutionResult> ExecuteReadWaitAsync(
        ReadWaitStep step, IPlcManager manager)
    {
        Log($"[readWait] 等待 {step.Address} 符合 {step.Condition}（超時 {step.MaxTimeoutMs}ms）");

        var elapsed = 0;
        var pollInterval = Math.Max(50, step.PollIntervalMs);
        var maxTimeout = Math.Max(100, step.MaxTimeoutMs);

        while (elapsed < maxTimeout)
        {
            var value = manager.ReadWord(step.Address);
            if (value is not null)
            {
                _variables[step.Address] = value.Value;
                if (EvaluateCondition(step.Condition))
                {
                    Log($"[readWait] 條件成立：{step.Condition} (值={value.Value}, 耗時 {elapsed}ms)");
                    return Ok();
                }
            }

            await Task.Delay(pollInterval);
            elapsed += pollInterval;
        }

        return Fail($"readWait 超時：{step.Address} 在 {maxTimeout}ms 內未符合 {step.Condition}");
    }

    // ── Expression evaluator ────────────────────────────────────────

    private static readonly Regex VarPattern = new(@"\$\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>解析變數引用並替換為值</summary>
    private string ResolveVariables(string expression)
    {
        return VarPattern.Replace(expression, match =>
        {
            var varName = match.Groups[1].Value;
            return _variables.TryGetValue(varName, out var val) ? val.ToString() : "0";
        });
    }

    /// <summary>
    /// 簡易條件評估器，支援: >, <, >=, <=, ==, !=
    /// 格式: "${variable} op value" 或 "value op value"
    /// </summary>
    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;

        var resolved = ResolveVariables(condition);

        // 嘗試解析 "left op right" 格式
        var match = Regex.Match(resolved.Trim(), @"^(-?\d+(?:\.\d+)?)\s*(>=|<=|!=|==|>|<)\s*(-?\d+(?:\.\d+)?)$");
        if (!match.Success)
        {
            // 單值判斷：非零即 true
            if (double.TryParse(resolved.Trim(), out var single))
                return single != 0;
            return false;
        }

        var left = double.Parse(match.Groups[1].Value);
        var op = match.Groups[2].Value;
        var right = double.Parse(match.Groups[3].Value);

        return op switch
        {
            ">"  => left > right,
            "<"  => left < right,
            ">=" => left >= right,
            "<=" => left <= right,
            "==" => Math.Abs(left - right) < 0.001,
            "!=" => Math.Abs(left - right) >= 0.001,
            _    => false,
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private void Log(string msg) => _executionLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");

    private static SequenceExecutionResult Ok(string msg = "")
        => new(true, msg);

    private static SequenceExecutionResult Fail(string msg)
        => new(false, msg);
}

/// <summary>序列執行結果</summary>
public sealed record SequenceExecutionResult(bool Success, string Message);
