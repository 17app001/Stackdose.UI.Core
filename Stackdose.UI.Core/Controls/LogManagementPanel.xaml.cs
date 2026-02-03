using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// LogManagementPanel - 日誌管理面板（可獨立使用）
    /// 支援 4 種日誌類型：AuditTrail, Operation, Event, PeriodicData
    /// </summary>
    public partial class LogManagementPanel : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly LogService _logService;
        private ObservableCollection<DateGroup> _dateGroups;
        private ObservableCollection<AuditTrailRecord> _currentAuditTrails;
        private ObservableCollection<DataLogRecord> _currentDataLogs;
        private ObservableCollection<OperationLogRecord> _currentOperationLogs;
        private ObservableCollection<EventLogRecord> _currentEventLogs;
        private ObservableCollection<EventLogRecord> _allEventLogs; // Store all event logs for filtering
        private ObservableCollection<PeriodicDataLogRecord> _currentPeriodicDataLogs;
        private DateGroup? _selectedDateGroup;
        private LogViewMode _currentViewMode = LogViewMode.AuditTrail;
        private DateTime _filterFromDate = DateTime.Today.AddDays(-7);
        private DateTime _filterToDate = DateTime.Today;
        
        // ?? 新增：標記是否為日期範圍查詢模式
        private bool _isDateRangeQueryMode = false;

        // Severity filter flags
        private bool _isSeverityAll = true;
        private bool _isSeverityCritical = false;
        private bool _isSeverityMajor = false;
        private bool _isSeverityMinor = false;
        private bool _isSeverityInfo = false;

        // Event type filter flags
        private bool _isEventTypeAlarm = false;
        private bool _isEventTypeWarning = false;
        private bool _isEventTypeSystem = false;
        private bool _isEventTypeSafety = false;

        #endregion

        #region Properties

        public ObservableCollection<DateGroup> DateGroups
        {
            get => _dateGroups;
            set { _dateGroups = value; OnPropertyChanged(); }
        }

        public ObservableCollection<AuditTrailRecord> CurrentAuditTrails
        {
            get => _currentAuditTrails;
            set { _currentAuditTrails = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRecordCount)); }
        }

        public ObservableCollection<OperationLogRecord> CurrentOperationLogs
        {
            get => _currentOperationLogs;
            set { _currentOperationLogs = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRecordCount)); }
        }

        public ObservableCollection<EventLogRecord> CurrentEventLogs
        {
            get => _currentEventLogs;
            set { _currentEventLogs = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRecordCount)); }
        }

        public ObservableCollection<PeriodicDataLogRecord> CurrentPeriodicDataLogs
        {
            get => _currentPeriodicDataLogs;
            set { _currentPeriodicDataLogs = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentRecordCount)); }
        }

        public DateGroup? SelectedDateGroup
        {
            get => _selectedDateGroup;
            set 
            { 
                _selectedDateGroup = value; 
                OnPropertyChanged();
                
                // ?? 只有在非日期範圍查詢模式時才載入單日數據
                if (!_isDateRangeQueryMode)
                {
                    LoadLogsForSelectedDate(); 
                }
            }
        }

        public LogViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                _currentViewMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAuditTrailMode));
                OnPropertyChanged(nameof(IsOperationMode));
                OnPropertyChanged(nameof(IsEventMode));
                OnPropertyChanged(nameof(IsPeriodicDataMode));
                OnPropertyChanged(nameof(CurrentViewModeText));
                OnPropertyChanged(nameof(CurrentRecordCount));
                
                // ?? 切換類型時重置為單日模式
                _isDateRangeQueryMode = false;
                
                // Reset severity filter when switching modes
                if (value == LogViewMode.Event)
                {
                    ResetSeverityFilter();
                }
                
                LoadLogsForSelectedDate();
            }
        }

        public bool IsAuditTrailMode
        {
            get => _currentViewMode == LogViewMode.AuditTrail;
            set { if (value) CurrentViewMode = LogViewMode.AuditTrail; }
        }

        public bool IsOperationMode
        {
            get => _currentViewMode == LogViewMode.Operation;
            set { if (value) CurrentViewMode = LogViewMode.Operation; }
        }

        public bool IsEventMode
        {
            get => _currentViewMode == LogViewMode.Event;
            set { if (value) CurrentViewMode = LogViewMode.Event; }
        }

        public bool IsPeriodicDataMode
        {
            get => _currentViewMode == LogViewMode.PeriodicData;
            set { if (value) CurrentViewMode = LogViewMode.PeriodicData; }
        }

        // Severity filter properties
        public bool IsSeverityAll
        {
            get => _isSeverityAll;
            set { _isSeverityAll = value; OnPropertyChanged(); }
        }

        public bool IsSeverityCritical
        {
            get => _isSeverityCritical;
            set { _isSeverityCritical = value; OnPropertyChanged(); }
        }

        public bool IsSeverityMajor
        {
            get => _isSeverityMajor;
            set { _isSeverityMajor = value; OnPropertyChanged(); }
        }

        public bool IsSeverityMinor
        {
            get => _isSeverityMinor;
            set { _isSeverityMinor = value; OnPropertyChanged(); }
        }

        public bool IsSeverityInfo
        {
            get => _isSeverityInfo;
            set { _isSeverityInfo = value; OnPropertyChanged(); }
        }

        // Event type filter properties
        public bool IsEventTypeAlarm
        {
            get => _isEventTypeAlarm;
            set { _isEventTypeAlarm = value; OnPropertyChanged(); }
        }

        public bool IsEventTypeWarning
        {
            get => _isEventTypeWarning;
            set { _isEventTypeWarning = value; OnPropertyChanged(); }
        }

        public bool IsEventTypeSystem
        {
            get => _isEventTypeSystem;
            set { _isEventTypeSystem = value; OnPropertyChanged(); }
        }

        public bool IsEventTypeSafety
        {
            get => _isEventTypeSafety;
            set { _isEventTypeSafety = value; OnPropertyChanged(); }
        }

        public string CurrentViewModeText => _currentViewMode switch
        {
            LogViewMode.AuditTrail => "AUDIT TRAIL",
            LogViewMode.Operation => "OPERATION LOG",
            LogViewMode.Event => "EVENT LOG",
            LogViewMode.PeriodicData => "PERIODIC DATA LOG",
            _ => "UNKNOWN"
        };

        public int CurrentRecordCount => _currentViewMode switch
        {
            LogViewMode.AuditTrail => CurrentAuditTrails.Count,
            LogViewMode.Operation => CurrentOperationLogs.Count,
            LogViewMode.Event => CurrentEventLogs.Count,
            LogViewMode.PeriodicData => CurrentPeriodicDataLogs.Count,
            _ => 0
        };

        public DateTime FilterFromDate
        {
            get => _filterFromDate;
            set { _filterFromDate = value; OnPropertyChanged(); }
        }

        public DateTime FilterToDate
        {
            get => _filterToDate;
            set { _filterToDate = value; OnPropertyChanged(); }
        }

        #endregion

        #region Constructor

        public LogManagementPanel()
        {
            InitializeComponent();
            DataContext = this;

            // 確保資料庫已初始化
            try
            {
                var _ = Stackdose.UI.Core.Helpers.ComplianceContext.CurrentUser;
                System.Diagnostics.Debug.WriteLine("[LogManagementPanel] ComplianceContext initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] ComplianceContext init error: {ex.Message}");
            }

            _logService = new LogService();
            _dateGroups = new ObservableCollection<DateGroup>();
            _currentAuditTrails = new ObservableCollection<AuditTrailRecord>();
            _currentDataLogs = new ObservableCollection<DataLogRecord>();
            _currentOperationLogs = new ObservableCollection<OperationLogRecord>();
            _currentEventLogs = new ObservableCollection<EventLogRecord>();
            _allEventLogs = new ObservableCollection<EventLogRecord>();
            _currentPeriodicDataLogs = new ObservableCollection<PeriodicDataLogRecord>();

            Loaded += LogManagementPanel_Loaded;
        }

        #endregion

        #region Event Handlers

        private void LogManagementPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDateGroups();
        }

        #endregion

        #region Severity Filter Methods

        private void ResetSeverityFilter()
        {
            _isSeverityAll = true;
            _isSeverityCritical = false;
            _isSeverityMajor = false;
            _isSeverityMinor = false;
            _isSeverityInfo = false;
            _isEventTypeAlarm = false;
            _isEventTypeWarning = false;
            _isEventTypeSystem = false;
            _isEventTypeSafety = false;
            
            // Notify all properties
            OnPropertyChanged(nameof(IsSeverityAll));
            OnPropertyChanged(nameof(IsSeverityCritical));
            OnPropertyChanged(nameof(IsSeverityMajor));
            OnPropertyChanged(nameof(IsSeverityMinor));
            OnPropertyChanged(nameof(IsSeverityInfo));
            OnPropertyChanged(nameof(IsEventTypeAlarm));
            OnPropertyChanged(nameof(IsEventTypeWarning));
            OnPropertyChanged(nameof(IsEventTypeSystem));
            OnPropertyChanged(nameof(IsEventTypeSafety));
        }

        private void ApplyEventLogFilter()
        {
            if (_allEventLogs == null || _allEventLogs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[LogManagementPanel] ApplyEventLogFilter: No event logs to filter");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] ApplyEventLogFilter: Total={_allEventLogs.Count}");
            System.Diagnostics.Debug.WriteLine($"  SeverityAll={_isSeverityAll}, Critical={_isSeverityCritical}, Major={_isSeverityMajor}, Minor={_isSeverityMinor}, Info={_isSeverityInfo}");
            System.Diagnostics.Debug.WriteLine($"  Alarm={_isEventTypeAlarm}, Warning={_isEventTypeWarning}, System={_isEventTypeSystem}, Safety={_isEventTypeSafety}");

            var filtered = _allEventLogs.AsEnumerable();

            // Apply severity filter
            if (!_isSeverityAll)
            {
                var severities = new System.Collections.Generic.List<string>();
                if (_isSeverityCritical) severities.Add("Critical");
                if (_isSeverityMajor) severities.Add("Major");
                if (_isSeverityMinor) severities.Add("Minor");
                if (_isSeverityInfo) severities.Add("Info");

                System.Diagnostics.Debug.WriteLine($"  Filtering by severities: {string.Join(", ", severities)}");

                if (severities.Count > 0)
                {
                    filtered = filtered.Where(e => severities.Any(s => 
                        string.Equals(e.Severity, s, StringComparison.OrdinalIgnoreCase)));
                }
            }

            // Apply event type filter
            bool hasEventTypeFilter = _isEventTypeAlarm || _isEventTypeWarning || _isEventTypeSystem || _isEventTypeSafety;
            if (hasEventTypeFilter)
            {
                var eventTypes = new System.Collections.Generic.List<string>();
                if (_isEventTypeAlarm) eventTypes.Add("Alarm");
                if (_isEventTypeWarning) eventTypes.Add("Warning");
                if (_isEventTypeSystem) eventTypes.Add("System Event");
                if (_isEventTypeSafety) eventTypes.Add("Safety Event");

                System.Diagnostics.Debug.WriteLine($"  Filtering by event types: {string.Join(", ", eventTypes)}");
                filtered = filtered.Where(e => eventTypes.Any(t => 
                    string.Equals(e.EventType, t, StringComparison.OrdinalIgnoreCase)));
            }

            var result = filtered.ToList();
            System.Diagnostics.Debug.WriteLine($"  Filter result: {result.Count} records");

            CurrentEventLogs.Clear();
            foreach (var r in result)
            {
                CurrentEventLogs.Add(r);
            }

            OnPropertyChanged(nameof(CurrentRecordCount));
        }

        private void SeverityAll_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[LogManagementPanel] SeverityAll_Click");
            
            _isSeverityAll = true;
            _isSeverityCritical = false;
            _isSeverityMajor = false;
            _isSeverityMinor = false;
            _isSeverityInfo = false;
            
            OnPropertyChanged(nameof(IsSeverityAll));
            OnPropertyChanged(nameof(IsSeverityCritical));
            OnPropertyChanged(nameof(IsSeverityMajor));
            OnPropertyChanged(nameof(IsSeverityMinor));
            OnPropertyChanged(nameof(IsSeverityInfo));
            
            ApplyEventLogFilter();
        }

        private void SeverityCritical_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] SeverityCritical_Click");
            
            _isSeverityAll = false;
            // Don't toggle here - the ToggleButton already toggled it via binding
            // Just apply filter with current state
            
            OnPropertyChanged(nameof(IsSeverityAll));
            
            // Use dispatcher to wait for binding update
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SeverityMajor_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] SeverityMajor_Click");
            
            _isSeverityAll = false;
            OnPropertyChanged(nameof(IsSeverityAll));
            
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SeverityMinor_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] SeverityMinor_Click");
            
            _isSeverityAll = false;
            OnPropertyChanged(nameof(IsSeverityAll));
            
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void SeverityInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] SeverityInfo_Click");
            
            _isSeverityAll = false;
            OnPropertyChanged(nameof(IsSeverityAll));
            
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void EventTypeAlarm_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] EventTypeAlarm_Click");
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void EventTypeWarning_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] EventTypeWarning_Click");
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void EventTypeSystem_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] EventTypeSystem_Click");
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void EventTypeSafety_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[LogManagementPanel] EventTypeSafety_Click");
            Dispatcher.InvokeAsync(() => ApplyEventLogFilter(), System.Windows.Threading.DispatcherPriority.Background);
        }

        #endregion

        #region Data Loading

        private void LoadDateGroups()
        {
            try
            {
                var dateGroups = _logService.GetDateGroups();
                DateGroups.Clear();
                foreach (var group in dateGroups)
                    DateGroups.Add(group);

                if (DateGroups.Count > 0)
                    SelectedDateGroup = DateGroups.First();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入日期列表失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLogsForSelectedDate()
        {
            if (SelectedDateGroup == null) return;

            try
            {
                switch (CurrentViewMode)
                {
                    case LogViewMode.AuditTrail:
                        var auditRecords = _logService.GetAuditTrailsByDate(SelectedDateGroup.Date);
                        CurrentAuditTrails.Clear();
                        foreach (var r in auditRecords) CurrentAuditTrails.Add(r);
                        break;

                    case LogViewMode.Operation:
                        var opRecords = _logService.GetOperationLogsByDate(SelectedDateGroup.Date);
                        CurrentOperationLogs.Clear();
                        foreach (var r in opRecords) CurrentOperationLogs.Add(r);
                        break;

                    case LogViewMode.Event:
                        var eventRecords = _logService.GetEventLogsByDate(SelectedDateGroup.Date);
                        _allEventLogs.Clear();
                        foreach (var r in eventRecords) _allEventLogs.Add(r);
                        ApplyEventLogFilter();
                        break;

                    case LogViewMode.PeriodicData:
                        var periodicRecords = _logService.GetPeriodicDataLogsByDate(SelectedDateGroup.Date);
                        CurrentPeriodicDataLogs.Clear();
                        foreach (var r in periodicRecords) CurrentPeriodicDataLogs.Add(r);
                        break;
                }
                
                // ?? 更新 CurrentRecordCount
                OnPropertyChanged(nameof(CurrentRecordCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"載入日誌失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Handlers

        private void DateItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is DateGroup dateGroup)
            {
                // ?? 重置為單日模式
                _isDateRangeQueryMode = false;
                SelectedDateGroup = dateGroup;
            }
        }

        private void AuditTrailButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentViewMode = LogViewMode.AuditTrail;
        }

        private void OperationButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentViewMode = LogViewMode.Operation;
        }

        private void EventButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentViewMode = LogViewMode.Event;
        }

        private void PeriodicDataButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentViewMode = LogViewMode.PeriodicData;
        }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FilterFromDate > FilterToDate)
                {
                    MessageBox.Show("起始日期不可大於結束日期", "日期錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ?? 設為日期範圍查詢模式
                _isDateRangeQueryMode = true;

                switch (CurrentViewMode)
                {
                    case LogViewMode.AuditTrail:
                        var auditRecords = _logService.GetAuditTrailsByDateRange(FilterFromDate, FilterToDate);
                        CurrentAuditTrails.Clear();
                        foreach (var r in auditRecords) CurrentAuditTrails.Add(r);
                        MessageBox.Show($"查詢完成！共 {CurrentAuditTrails.Count} 筆審計軌跡記錄", "查詢結果", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case LogViewMode.Operation:
                        var opRecords = _logService.GetOperationLogsByDateRange(FilterFromDate, FilterToDate);
                        CurrentOperationLogs.Clear();
                        foreach (var r in opRecords) CurrentOperationLogs.Add(r);
                        MessageBox.Show($"查詢完成！共 {CurrentOperationLogs.Count} 筆操作日誌記錄", "查詢結果", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case LogViewMode.Event:
                        var eventRecords = _logService.GetEventLogsByDateRange(FilterFromDate, FilterToDate);
                        _allEventLogs.Clear();
                        foreach (var r in eventRecords) _allEventLogs.Add(r);
                        ApplyEventLogFilter();
                        MessageBox.Show($"查詢完成！共 {CurrentEventLogs.Count} 筆事件日誌記錄", "查詢結果", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                    case LogViewMode.PeriodicData:
                        var periodicRecords = _logService.GetPeriodicDataLogsByDateRange(FilterFromDate, FilterToDate);
                        CurrentPeriodicDataLogs.Clear();
                        foreach (var r in periodicRecords) CurrentPeriodicDataLogs.Add(r);
                        MessageBox.Show($"查詢完成！共 {CurrentPeriodicDataLogs.Count} 筆週期數據記錄", "查詢結果", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }

                // ?? 更新 CurrentRecordCount
                OnPropertyChanged(nameof(CurrentRecordCount));
                
                // ?? 不再呼叫 LoadDateGroups()，避免重置數據
                // LoadDateGroups();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查詢失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int recordCount = CurrentRecordCount;
                if (recordCount == 0)
                {
                    MessageBox.Show("目前沒有可匯出的記錄", "無資料", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF 檔案 (*.pdf)|*.pdf",
                    FileName = $"Log_{CurrentViewModeText}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    switch (CurrentViewMode)
                    {
                        case LogViewMode.AuditTrail:
                            _logService.ExportAuditTrailsToPdf(CurrentAuditTrails.ToList(), saveDialog.FileName);
                            break;

                        case LogViewMode.Operation:
                            // TODO: 實作 Operation 匯出
                            break;

                        case LogViewMode.Event:
                            // TODO: 實作 Event 匯出
                            break;

                        case LogViewMode.PeriodicData:
                            // TODO: 實作 PeriodicData 匯出
                            break;
                    }

                    MessageBox.Show($"成功匯出 {recordCount} 筆記錄至：\n{saveDialog.FileName}", "匯出成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    var result = MessageBox.Show("是否要開啟匯出的 PDF 檔案？", "開啟檔案", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"匯出失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// 日誌檢視模式（4 種類型）
    /// </summary>
    public enum LogViewMode
    {
        AuditTrail,   // 審計軌跡
        Operation,    // 操作日誌
        Event,        // 事件日誌
        PeriodicData  // 週期性數據
    }
}
