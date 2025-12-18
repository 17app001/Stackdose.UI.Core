using Stackdose.UI.Core.Helpers;
using System.Windows;
using System.Windows.Input;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者登入對話框
    /// </summary>
    public partial class LoginDialog : Window
    {
        public bool LoginSuccessful { get; private set; } = false;

        public LoginDialog()
        {
            InitializeComponent();

            // 支援 Enter 鍵登入
            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    LoginButton_Click(null, null);
                }
                else if (e.Key == Key.Escape)
                {
                    CancelButton_Click(null, null);
                }
            };

            // 自動 focus 到帳號欄位
            this.Loaded += (s, e) => UserIdTextBox.Focus();
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs? e)
        {
            // 清除錯誤訊息
            ErrorText.Visibility = Visibility.Collapsed;

            // 取得輸入
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // 驗證輸入
            if (string.IsNullOrWhiteSpace(userId))
            {
                ShowError("請輸入帳號 (Please enter User ID)");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("請輸入密碼 (Please enter Password)");
                return;
            }

            // 嘗試登入
            bool success = SecurityContext.Login(userId, password);

            if (success)
            {
                LoginSuccessful = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                ShowError("登入失敗：帳號或密碼錯誤\nLogin Failed: Invalid User ID or Password");
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs? e)
        {
            LoginSuccessful = false;
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 顯示登入對話框
        /// </summary>
        /// <returns>是否登入成功</returns>
        public static bool ShowLoginDialog()
        {
            var dialog = new LoginDialog();
            bool? result = dialog.ShowDialog();
            return result == true && dialog.LoginSuccessful;
        }
    }
}
