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
using Stackdose.UI.Core.Controls.Base;
using Stackdose.Abstractions.Hardware;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 報警檢視器控制項
    /// </summary>
    public partial class AlarmViewer : PlcControlBase
    {
        #region Private Fields

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

        public static readonly DependencyProperty ItemFontSizeProperty =
            DependencyProperty.Register(nameof(ItemFontSize), typeof(double), typeof(AlarmViewer),
                new PropertyMetadata(14.0));

        public double ItemFontSize
        {
            get => (double)GetValue(ItemFontSizeProperty);
            set => SetValue(ItemFontSizeProperty, value);
        }

        public static readonly DependencyProperty ShowDeviceAddressProperty =
            DependencyProperty.Register(nameof(ShowDeviceAddress), typeof(bool), typeof(AlarmViewer),
                new PropertyMetadata(true));

        public bool ShowDeviceAddress
        {
            get => (bool)GetValue(ShowDeviceAddressProperty);
            set => SetValue(ShowDeviceAddressProperty, value);
        }

        public static readonly DependencyProperty ConfigFileProperty =
            DependencyProperty.Register(nameof(ConfigFile), typeof(string), typeof(AlarmViewer),
                new PropertyMetadata(null, OnConfigFileChanged));

        public string ConfigFile
        {
            get => (string)GetValue(ConfigFileProperty);
            set => SetValue(ConfigFileProperty, value);
        }

        public static readonly DependencyProperty DefaultShowActiveOnlyProperty =
            DependencyProperty.Register(nameof(DefaultShowActiveOnly), typeof(bool), typeof(AlarmViewer),
                new PropertyMetadata(true));

        public bool DefaultShowActiveOnly
        {
            get => (bool)GetValue(DefaultShowActiveOnlyProperty);
            set => SetValue(DefaultShowActiveOnlyProperty, value);
        }

        #endregion

        #region Constructor

        public AlarmViewer()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        protected override void OnPlcControlLoaded()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;
            ChkShowAlarmsOnly.IsChecked = DefaultShowActiveOnly;
            _showAlarmsOnly = DefaultShowActiveOnly;
            LoadAlarmsFromConfig();
            var manager = PlcContext.GlobalStatus?.CurrentManager;
            if (manager != null && manager.IsConnected)
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                    new System.Action(() => RefreshFrom(manager)));
        }

        protected override void OnPlcControlUnloaded() { }

        protected override void OnPlcConnected(IPlcManager manager)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Action(() => RefreshFrom(manager)));
        }

        protected override void OnPlcDataUpdated(IPlcManager manager)
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new System.Action(() => RefreshFrom(manager)));
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

        #endregion

        #region Private Methods

        private void RefreshFrom(Abstractions.Hardware.IPlcManager manager)
        {
            if (manager == null || _allAlarms.Count == 0) return;

            int activeCount = 0;
            var wordCache = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

            foreach (var alarm in _allAlarms)
            {
                if (!wordCache.TryGetValue(alarm.Device, out var wordValue))
                {
                    wordValue = manager.ReadWord(alarm.Device);
                    wordCache[alarm.Device] = wordValue;
                }
                var newIsActive = wordValue.HasValue && ((wordValue.Value >> alarm.Bit) & 1) == 1;
                if (alarm.IsActive != newIsActive)
                {
                    alarm.IsActive = newIsActive;
                    alarm.LastUpdate = DateTime.Now;
                }

                if (newIsActive) { activeCount++; }
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

