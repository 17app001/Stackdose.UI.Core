using Stackdose.Abstractions.Hardware;
using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.ModelB.Pages
{
    public partial class HomePage : UserControl
    {
        public event EventHandler<string>? MachineSelected;

        public HomePage()
        {
            InitializeComponent();
            
            this.Loaded += HomePage_Loaded;
            this.Unloaded += HomePage_Unloaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            var globalStatus = PlcContext.GlobalStatus;
            if (globalStatus != null)
            {
                globalStatus.ConnectionEstablished += OnPlcConnectionEstablished;
                globalStatus.ScanUpdated += OnPlcScanUpdated;
                
                UpdatePlcStatus(globalStatus.CurrentManager?.IsConnected ?? false);
            }
            
            ComplianceContext.LogSystem("Home page loaded", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            var globalStatus = PlcContext.GlobalStatus;
            if (globalStatus != null)
            {
                globalStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
                globalStatus.ScanUpdated -= OnPlcScanUpdated;
            }
        }

        private void OnPlcConnectionEstablished(IPlcManager manager)
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdatePlcStatus(true);
                ComplianceContext.LogSystem("PLC connection established", Stackdose.Abstractions.Logging.LogLevel.Success, showInUi: true);
            });
        }

        private void OnPlcScanUpdated(IPlcManager manager)
        {
            // TODO: Read PLC data and update machine cards
        }

        private void UpdatePlcStatus(bool isConnected)
        {
            string status = isConnected ? "Connected" : "Disconnected";
            
            Machine1Card.RightBottomValue = status;
            Machine2Card.RightBottomValue = status;
        }

        private void OnMachineSelected(object sender, string machineId)
        {
            ComplianceContext.LogSystem($"Machine selected: {machineId}", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
            MachineSelected?.Invoke(this, machineId);
        }
    }
}
