using System.Windows;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Text input dialog for user-provided values.
    /// </summary>
    public partial class InputDialog : Window
    {
        public string DialogTitle { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string InputText => InputTextBox.Text;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            DialogTitle = title;
            Prompt = prompt;
            DataContext = this;

            Loaded += (_, _) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                CyberMessageBox.Show("請輸入內容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
