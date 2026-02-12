using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.ModelB.Pages
{
    public partial class MachineControlPage : UserControl
    {
        public event EventHandler? BackRequested;

        private string _machineId = "";

        public MachineControlPage()
        {
            InitializeComponent();
        }

        public void SetMachineId(string machineId)
        {
            _machineId = machineId;
            TitleText.Text = machineId;
            SubtitleText.Text = $"{machineId} Detail Control";
            
            ComplianceContext.LogSystem($"Entered {machineId} control page", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ComplianceContext.LogSystem($"Left {_machineId} control page", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
