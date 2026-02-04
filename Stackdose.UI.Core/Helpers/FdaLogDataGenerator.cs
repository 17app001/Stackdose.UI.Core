using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// FDA 21 CFR Part 11 Compliant Test Data Generator
    /// </summary>
    public static class FdaLogDataGenerator
    {
        private static readonly Random _random = new Random();
        private static bool _initialized = false;

        #region Event Log Data Definitions

        private static readonly string[] DeviceStates = new[]
        {
            "Ready", "Running", "Idle", "Error", "Paused", "Maintenance", "Stopped"
        };

        private static readonly List<EventTemplate> EventTemplates = new()
        {
            // Critical Events
            new EventTemplate { EventType = "Safety Event", EventCode = "SE-001", EventDescription = "Emergency Stop Activated", Severity = "Critical", MessageTemplate = "Emergency stop button pressed at station {0}. System halted immediately.", RequiresUserId = true },
            new EventTemplate { EventType = "Alarm", EventCode = "AL-001", EventDescription = "Temperature Critical High", Severity = "Critical", MessageTemplate = "Dry module temperature exceeded critical threshold: {0}C (Max: 85C)", RequiresUserId = false },
            new EventTemplate { EventType = "Alarm", EventCode = "AL-002", EventDescription = "Pressure Loss Critical", Severity = "Critical", MessageTemplate = "CDA inlet pressure dropped to {0} bar (Min: 5.0 bar). System shutdown initiated.", RequiresUserId = false },

            // Major Events
            new EventTemplate { EventType = "Alarm", EventCode = "AL-003", EventDescription = "Temperature Warning High", Severity = "Major", MessageTemplate = "Pre-dry temperature reached {0}C (Warning threshold: 75C)", RequiresUserId = false },
            new EventTemplate { EventType = "Safety Event", EventCode = "SE-002", EventDescription = "Safety Door Opened During Operation", Severity = "Major", MessageTemplate = "Safety door opened during active batch. Production paused automatically.", RequiresUserId = true },
            new EventTemplate { EventType = "System Event", EventCode = "SY-001", EventDescription = "PLC Connection Lost", Severity = "Major", MessageTemplate = "Lost communication with PLC at {0}. Attempting reconnection...", RequiresUserId = false },

            // Minor Events
            new EventTemplate { EventType = "Warning", EventCode = "WN-001", EventDescription = "Temperature Approaching Limit", Severity = "Minor", MessageTemplate = "Dry module temperature: {0}C approaching warning threshold (75C)", RequiresUserId = false },
            new EventTemplate { EventType = "Warning", EventCode = "WN-002", EventDescription = "Low Consumable Level", Severity = "Minor", MessageTemplate = "Ink cartridge level below 20%. Replacement recommended within 50 prints.", RequiresUserId = false },
            new EventTemplate { EventType = "System Event", EventCode = "SY-002", EventDescription = "Maintenance Schedule Due", Severity = "Minor", MessageTemplate = "Scheduled maintenance due in {0} hours. Please plan accordingly.", RequiresUserId = false },

            // Info Events
            new EventTemplate { EventType = "System Event", EventCode = "SY-003", EventDescription = "Batch Completed Successfully", Severity = "Info", MessageTemplate = "Batch {0} completed. Total units: {1}. Quality check: Passed.", RequiresUserId = false },
            new EventTemplate { EventType = "System Event", EventCode = "SY-004", EventDescription = "System Startup", Severity = "Info", MessageTemplate = "System initialized successfully. All subsystems operational.", RequiresUserId = false },
            new EventTemplate { EventType = "System Event", EventCode = "SY-005", EventDescription = "Parameter Auto-Adjustment", Severity = "Info", MessageTemplate = "Temperature compensation applied. Ambient: {0}C, Adjustment: +{1}C", RequiresUserId = false }
        };

        #endregion

        #region Initialization

        /// <summary>
        /// Ensure database is initialized
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;

            try
            {
                // Initialize SqliteLogger (creates tables if not exist)
                SqliteLogger.Initialize();
                _initialized = true;
                System.Diagnostics.Debug.WriteLine("[FdaLogDataGenerator] Database initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Initialize Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generate Event Log test data
        /// </summary>
        public static void GenerateEventLogs(int count = 100, string batchId = "", DateTime? startDate = null, int daysBack = 7)
        {
            EnsureInitialized();

            var start = startDate ?? DateTime.Now;
            var end = start.AddDays(-daysBack);

            System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Generating {count} Event Log records...");

            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var template = EventTemplates[_random.Next(EventTemplates.Count)];
                    var timestamp = RandomDateTime(end, start);
                    var currentState = DeviceStates[_random.Next(DeviceStates.Length)];
                    var userId = template.RequiresUserId ? $"USER{_random.Next(1, 6):D3}" : string.Empty;
                    var message = GenerateMessage(template.MessageTemplate);
                    var finalBatchId = string.IsNullOrEmpty(batchId) ? $"BATCH-{timestamp:yyyyMMdd}-{_random.Next(1000, 9999)}" : batchId;

                    conn.Execute(
                        @"INSERT INTO EventLogs (Timestamp, BatchId, EventType, EventCode, EventDescription, Severity, CurrentState, UserId, Message) 
                          VALUES (@Timestamp, @BatchId, @EventType, @EventCode, @EventDescription, @Severity, @CurrentState, @UserId, @Message)",
                        new
                        {
                            Timestamp = timestamp,
                            BatchId = finalBatchId,
                            EventType = template.EventType,
                            EventCode = template.EventCode,
                            EventDescription = template.EventDescription,
                            Severity = template.Severity,
                            CurrentState = currentState,
                            UserId = userId,
                            Message = message
                        },
                        transaction
                    );

                    if ((i + 1) % 20 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Generated {i + 1}/{count} Event Logs");
                    }
                }

                transaction.Commit();
                System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Event Log generation complete! Total: {count}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate Periodic Data Log test data
        /// </summary>
        public static void GeneratePeriodicDataLogs(int count = 500, string batchId = "", DateTime? startDate = null, int daysBack = 7)
        {
            EnsureInitialized();

            var start = startDate ?? DateTime.Now;
            var end = start.AddDays(-daysBack);

            System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Generating {count} Periodic Data Log records...");

            double basePredryTemp = 65.0;
            double baseDryTemp = 78.0;
            double basePressure = 6.2;

            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StackDoseData.db");
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var timestamp = RandomDateTime(end, start);
                    var finalBatchId = string.IsNullOrEmpty(batchId) ? $"BATCH-{timestamp:yyyyMMdd}-{_random.Next(1000, 9999)}" : batchId;

                    // ?? Generate random test user in FDA compliant format "UID-XXXXXX (DisplayName)"
                    var userIndex = _random.Next(1, 6); // USER001-USER005
                    var userId = $"UID-{userIndex:D6} (Test User {userIndex})";

                    var predryTemp = basePredryTemp + (_random.NextDouble() * 10 - 5);
                    var dryTemp = baseDryTemp + (_random.NextDouble() * 8 - 4);
                    var cdaPressure = basePressure + (_random.NextDouble() * 1.0 - 0.5);

                    // 5% chance of anomaly
                    if (_random.NextDouble() < 0.05)
                    {
                        if (_random.Next(2) == 0)
                            dryTemp += _random.NextDouble() * 10;
                        else
                            cdaPressure -= _random.NextDouble() * 1.5;
                    }

                    predryTemp = Math.Max(55.0, Math.Min(80.0, predryTemp));
                    dryTemp = Math.Max(65.0, Math.Min(90.0, dryTemp));
                    cdaPressure = Math.Max(4.5, Math.Min(7.5, cdaPressure));

                    // ?? Now includes UserId for FDA 21 CFR Part 11 compliance
                    conn.Execute(
                        @"INSERT INTO PeriodicDataLogs (Timestamp, BatchId, UserId, PredryTemp, DryTemp, CdaInletPressure) 
                          VALUES (@Timestamp, @BatchId, @UserId, @PredryTemp, @DryTemp, @CdaInletPressure)",
                        new
                        {
                            Timestamp = timestamp,
                            BatchId = finalBatchId,
                            UserId = userId,
                            PredryTemp = Math.Round(predryTemp, 2),
                            DryTemp = Math.Round(dryTemp, 2),
                            CdaInletPressure = Math.Round(cdaPressure, 3)
                        },
                        transaction
                    );

                    if ((i + 1) % 100 == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Generated {i + 1}/{count} Periodic Data Logs");
                    }
                }

                transaction.Commit();
                System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Periodic Data Log generation complete! Total: {count}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                System.Diagnostics.Debug.WriteLine($"[FdaLogDataGenerator] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate complete FDA test data set
        /// </summary>
        public static void GenerateCompleteFdaTestData(int eventLogCount = 100, int periodicDataCount = 500, int daysBack = 7)
        {
            System.Diagnostics.Debug.WriteLine("================================================================================");
            System.Diagnostics.Debug.WriteLine(" FDA 21 CFR Part 11 Test Data Generator");
            System.Diagnostics.Debug.WriteLine("================================================================================");

            GenerateEventLogs(eventLogCount, daysBack: daysBack);
            GeneratePeriodicDataLogs(periodicDataCount, daysBack: daysBack);

            System.Diagnostics.Debug.WriteLine("================================================================================");
            System.Diagnostics.Debug.WriteLine(" Test Data Generation Complete!");
            System.Diagnostics.Debug.WriteLine($" Event Logs: {eventLogCount}");
            System.Diagnostics.Debug.WriteLine($" Periodic Data Logs: {periodicDataCount}");
            System.Diagnostics.Debug.WriteLine($" Date Range: {DateTime.Now.AddDays(-daysBack):yyyy-MM-dd} ~ {DateTime.Now:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine("================================================================================");
        }

        #endregion

        #region Private Helper Methods

        private static DateTime RandomDateTime(DateTime start, DateTime end)
        {
            var range = (end - start).TotalSeconds;
            var randomSeconds = _random.NextDouble() * Math.Abs(range);
            return start.AddSeconds(randomSeconds);
        }

        private static string GenerateMessage(string template)
        {
            var paramCount = template.Count(c => c == '{');
            if (paramCount == 0) return template;

            var parameters = new object[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                parameters[i] = i switch
                {
                    0 => _random.Next(1, 10),
                    1 => _random.Next(50, 200),
                    2 => Math.Round(_random.NextDouble() * 5, 1),
                    _ => _random.Next(1, 100)
                };
            }

            if (template.Contains("C") && !template.Contains("CDA"))
                parameters[0] = Math.Round(70 + _random.NextDouble() * 15, 1);
            else if (template.Contains("bar"))
                parameters[0] = Math.Round(4.0 + _random.NextDouble() * 1.5, 2);

            return string.Format(template, parameters);
        }

        #endregion

        #region Nested Classes

        private class EventTemplate
        {
            public string EventType { get; set; } = "";
            public string EventCode { get; set; } = "";
            public string EventDescription { get; set; } = "";
            public string Severity { get; set; } = "";
            public string MessageTemplate { get; set; } = "";
            public bool RequiresUserId { get; set; }
        }

        #endregion
    }
}
