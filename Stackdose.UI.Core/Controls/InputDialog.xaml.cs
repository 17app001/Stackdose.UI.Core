using System.Windows;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 輸入對話框
    /// </summary>
    public partial class InputDialog : Window
    {
        public string Title { get; set; }
        public string Prompt { get; set; }
        public string InputText => InputTextBox.Text;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
            Prompt = prompt;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                MessageBox.Show("請輸入內容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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