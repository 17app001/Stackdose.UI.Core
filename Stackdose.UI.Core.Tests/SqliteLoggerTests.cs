using Microsoft.Data.Sqlite;
using Stackdose.UI.Core.Helpers;
using Xunit;
using System.Reflection;

namespace Stackdose.UI.Core.Tests;

public sealed class SqliteLoggerTests : IDisposable
{
    private readonly string _dbPath;

    public SqliteLoggerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"stackdose-ui-core-tests-{Guid.NewGuid():N}.db");
        ResetAndInitializeLogger(_dbPath);
    }

    [Fact]
    public void BatchThreshold_ShouldAutoFlush_DataLogs()
    {
        SqliteLogger.ConfigureBatch(batchSize: 2, flushIntervalMs: 60_000);

        SqliteLogger.LogData("Temperature", "D100", "70");
        SqliteLogger.LogData("Temperature", "D100", "71");

        Assert.Equal(2, QueryCount("DataLogs"));

        var stats = SqliteLogger.GetStatistics();
        Assert.True(stats.DataLogs >= 2);
        Assert.Equal(0, stats.PendingDataLogs);
    }

    [Fact]
    public void FlushAll_ShouldPersistPendingEntries()
    {
        SqliteLogger.ConfigureBatch(batchSize: 100, flushIntervalMs: 60_000);

        SqliteLogger.LogAudit("UID-000001 (Admin)", "WRITE", "Heater(D100)", "20", "30", "Manual", "Temp", "B001");
        SqliteLogger.LogOperation("UID-000001 (Admin)", "Start", "System", "Idle", "Run", "Started", "B001");

        Assert.Equal(0, QueryCount("AuditTrails"));
        Assert.Equal(0, QueryCount("OperationLogs"));

        SqliteLogger.FlushAll();

        Assert.Equal(1, QueryCount("AuditTrails"));
        Assert.Equal(1, QueryCount("OperationLogs"));
    }

    [Fact]
    public void Shutdown_ShouldFlushPendingPeriodicData()
    {
        SqliteLogger.ConfigureBatch(batchSize: 100, flushIntervalMs: 60_000);

        SqliteLogger.LogPeriodicData("B001", "UID-000001 (Admin)", 75.5, 60.2, 1.23);
        Assert.Equal(0, QueryCount("PeriodicDataLogs"));

        SqliteLogger.Shutdown();

        Assert.Equal(1, QueryCount("PeriodicDataLogs"));
    }

    public void Dispose()
    {
        SqliteLogger.Shutdown();
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Best effort cleanup in test environment.
            }
        }
    }

    private static int QueryCount(string tableName)
    {
        var connString = GetPrivateStringField("_connectionString");
        using var connection = new SqliteConnection(connString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName}";
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static void ResetAndInitializeLogger(string dbPath)
    {
        SqliteLogger.Shutdown();
        SetPrivateField("_dbPath", dbPath);
        SetPrivateField("_connectionString", $"Data Source={dbPath};Pooling=False");
        SqliteLogger.ResetStatistics();
        SqliteLogger.Initialize();
    }

    private static string GetPrivateStringField(string fieldName)
    {
        var field = typeof(SqliteLogger).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");

        return (string)(field.GetValue(null)
            ?? throw new InvalidOperationException($"Field '{fieldName}' has null value."));
    }

    private static void SetPrivateField(string fieldName, string value)
    {
        var field = typeof(SqliteLogger).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found.");

        field.SetValue(null, value);
    }
}
