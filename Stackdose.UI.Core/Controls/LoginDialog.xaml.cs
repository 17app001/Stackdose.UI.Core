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
            ErrorPanel.Visibility = Visibility.Collapsed;

            // 取得輸入
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // 驗證輸入
            if (string.IsNullOrWhiteSpace(userId))
            {
                ShowError("請輸入帳號 (Please enter User ID)");
                UserIdTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("請輸入密碼 (Please enter Password)");
                PasswordBox.Focus();
                return;
            }

            // ?? 修正：更詳細的登入驗證與錯誤訊息
            try
            {
                bool success = SecurityContext.Login(userId, password);

                if (success)
                {
                    LoginSuccessful = true;
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    // 登入失敗，顯示更具體的錯誤訊息
                    ShowError($"登入失敗 Login Failed\n帳號: {userId}\n\n可能原因：\n? 帳號不存在 (User not found)\n? 密碼錯誤 (Wrong password)\n? 帳號已停用 (Account inactive)");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"登入錯誤 Login Error:\n{ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login Error: {ex.Message}");
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
            ErrorPanel.Visibility = Visibility.Visible;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoginDialog] Show Error: {message}");
            #endif
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
