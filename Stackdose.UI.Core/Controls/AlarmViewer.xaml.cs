using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Stackdose.UI.Core.Helpers;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 報警檢視器控制項
    /// </summary>
    /// <remarks>
    /// 提供 PLC 報警監控功能，支援：
    /// - JSON 設定檔載入
    /// - 即時報警狀態更新
    /// - 分組顯示
    /// - 僅顯示觸發報警過濾
    /// </remarks>
    public partial class AlarmViewer : UserControl
    {
        #region Private Fields

        private PlcStatus? _boundStatus;
        private List<AlarmItem> _allAlarms = new();
        private bool _showAlarmsOnly = false;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(AlarmViewer), 
                new PropertyMetadata("ALARM VIEWER"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ConfigFileProperty =
            DependencyProperty.Register(nameof(ConfigFile), typeof(string), typeof(AlarmViewer),
                new PropertyMetadata(null, OnConfigFileChanged));

        public string ConfigFile
        {
            get => (string)GetValue(ConfigFileProperty);
            set => SetValue(ConfigFileProperty, value);
        }

        public static readonly DependencyProperty TargetStatusProperty =
            DependencyProperty.Register(nameof(TargetStatus), typeof(PlcStatus), typeof(AlarmViewer),
                new PropertyMetadata(null, OnTargetStatusChanged));

        public PlcStatus TargetStatus
        {
            get => (PlcStatus)GetValue(TargetStatusProperty);
            set => SetValue(TargetStatusProperty, value);
        }

        #endregion

        #region Constructor

        public AlarmViewer()
        {
            InitializeComponent();
            this.Loaded += AlarmViewer_Loaded;
            this.Unloaded += AlarmViewer_Unloaded;
        }

        #endregion

        #region Event Handlers

        private void AlarmViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            LoadAlarmsFromConfig();

            if (TargetStatus == null)
            {
                var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
                if (contextStatus != null) BindToStatus(contextStatus);
            }
        }

        private void AlarmViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
                _boundStatus = null;
            }
        }

        private void ChkShowAlarmsOnly_Changed(object sender, RoutedEventArgs e)
        {
            _showAlarmsOnly = ChkShowAlarmsOnly.IsChecked == true;
            UpdateAlarmListDisplay();
        }

        #endregion

        #region Property Change Callbacks

        private static void OnConfigFileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AlarmViewer viewer)
            {
                viewer.LoadAlarmsFromConfig();
            }
        }

        private static void OnTargetStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AlarmViewer viewer && e.NewValue is PlcStatus newStatus)
            {
                viewer.BindToStatus(newStatus);
            }
        }

        #endregion

        #region Private Methods

        private void BindToStatus(PlcStatus? newStatus)
        {
            if (_boundStatus == newStatus) return;

            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated -= OnScanUpdated;
            }

            _boundStatus = newStatus;

            if (_boundStatus != null)
            {
                _boundStatus.ScanUpdated += OnScanUpdated;
            }
        }

        private void OnScanUpdated(Abstractions.Hardware.IPlcManager manager)
        {
            try
            {
                if (Dispatcher.HasShutdownStarted) return;

                Dispatcher.Invoke(() =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        RefreshFrom(manager);
                    }
                });
            }
            catch { }
        }

        private void RefreshFrom(Abstractions.Hardware.IPlcManager manager)
        {
            if (manager == null || _allAlarms.Count == 0) return;

            int activeCount = 0;

            foreach (var alarm in _allAlarms)
            {
                // 讀取 Word，然後取出指定 Bit
                var wordValue = manager.ReadWord(alarm.Device);
                if (wordValue.HasValue)
                {
                    alarm.IsActive = ((wordValue.Value >> alarm.Bit) & 1) == 1;
                }
                else
                {
                    alarm.IsActive = false;
                }
                
                alarm.LastUpdate = DateTime.Now;

                if (alarm.IsActive)
                {
                    activeCount++;
                }
            }

            TxtAlarmCount.Text = activeCount.ToString();
            UpdateAlarmListDisplay();
        }

        private void LoadAlarmsFromConfig()
        {
            _allAlarms.Clear();

            if (string.IsNullOrWhiteSpace(ConfigFile))
            {
                ShowNoDataHint(true);
                return;
            }

            try
            {
                var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFile);

                if (!File.Exists(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[AlarmViewer] Config file not found: {fullPath}");
                    ShowNoDataHint(true);
                    return;
                }

                var json = File.ReadAllText(fullPath);
                var config = JsonSerializer.Deserialize<AlarmConfig>(json);

                if (config?.Alarms != null)
                {
                    _allAlarms = config.Alarms;
                    TxtTotalCount.Text = _allAlarms.Count.ToString();
                    TxtAlarmCount.Text = "0";

                    UpdateAlarmListDisplay();
                    ShowNoDataHint(false);
                }
                else
                {
                    ShowNoDataHint(true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlarmViewer] LoadConfig Error: {ex.Message}");
                ShowNoDataHint(true);
            }
        }

        private void UpdateAlarmListDisplay()
        {
            var displayList = _showAlarmsOnly
                ? _allAlarms.Where(a => a.IsActive).ToList()
                : _allAlarms;

            var groupedView = CollectionViewSource.GetDefaultView(displayList);
            groupedView.GroupDescriptions.Clear();
            groupedView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AlarmItem.Group)));

            AlarmList.ItemsSource = groupedView;
        }

        private void ShowNoDataHint(bool show)
        {
            NoDataHint.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Nested Classes

        private class AlarmConfig
        {
            public List<AlarmItem> Alarms { get; set; } = new();
        }

        #endregion
    }

    /// <summary>
    /// 報警項目資料模型
    /// </summary>
    public class AlarmItem : INotifyPropertyChanged
    {
        private bool _isActive;
        private DateTime _lastUpdate;

        public string Group { get; set; } = "General";
        public string Device { get; set; } = "";
        public int Bit { get; set; }
        public string OperationDescription { get; set; } = "";

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set
            {
                if (_lastUpdate != value)
                {
                    _lastUpdate = value;
                    OnPropertyChanged(nameof(LastUpdate));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
