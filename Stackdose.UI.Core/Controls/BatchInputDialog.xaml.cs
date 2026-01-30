using System.Windows;
using System.Windows.Input;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Batch Input Dialog - 批次編號輸入對話框
    /// </summary>
    /// <remarks>
    /// 用於在啟動製程前輸入批次編號
    /// 符合 FDA 21 CFR Part 11 規範要求
    /// </remarks>
    public partial class BatchInputDialog : Window
    {
        /// <summary>
        /// 取得輸入的批次編號
        /// </summary>
        public string BatchNumber { get; private set; } = string.Empty;

        /// <summary>
        /// 建構函數
        /// </summary>
        public BatchInputDialog()
        {
            InitializeComponent();
            
            // 自動聚焦到輸入框
            Loaded += (s, e) =>
            {
                BatchNumberTextBox.Focus();
                BatchNumberTextBox.SelectionStart = BatchNumberTextBox.Text.Length;
            };
        }

        /// <summary>
        /// 建構函數（自訂預設批次編號）
        /// </summary>
        /// <param name="defaultBatchNumber">預設批次編號</param>
        public BatchInputDialog(string defaultBatchNumber) : this()
        {
            if (!string.IsNullOrWhiteSpace(defaultBatchNumber))
            {
                BatchNumberTextBox.Text = defaultBatchNumber;
            }
        }

        /// <summary>
        /// 確定按鈕點擊事件
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                BatchNumber = BatchNumberTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// 取消按鈕點擊事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 輸入框按鍵事件（支援 Enter 確認）
        /// </summary>
        private void BatchNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }

        /// <summary>
        /// 驗證輸入
        /// </summary>
        /// <returns>是否有效</returns>
        private bool ValidateInput()
        {
            string input = BatchNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show(
                    "Batch number cannot be empty!\n批次編號不能為空！",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                BatchNumberTextBox.Focus();
                return false;
            }

            if (input.Length < 5)
            {
                MessageBox.Show(
                    "Batch number must be at least 5 characters!\n批次編號至少需要 5 個字元！",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                BatchNumberTextBox.Focus();
                return false;
            }

            return true;
        }
    }
}
