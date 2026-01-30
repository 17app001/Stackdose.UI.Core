using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Services;
using System.Windows;
using System.Windows.Input;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者登入對話視窗
    /// </summary>
    public partial class LoginDialog : Window
    {
        public bool LoginSuccessful { get; private set; } = false;

        public LoginDialog()
        {
            InitializeComponent();

            #if DEBUG
            // DEBUG mode: Auto-fill admin01 (for testing)
            System.Diagnostics.Debug.WriteLine("[LoginDialog] DEBUG Mode: Auto-filling admin01 credentials");
            UserIdTextBox.Text = "admin01";
            // Note: Password is not pre-filled for security
            System.Diagnostics.Debug.WriteLine("[LoginDialog] Tip: Use password 'admin123' to login");
            #else
            // RELEASE mode: Auto-fill current Windows username
            try
            {
                string currentUser = AdAuthenticationService.GetCurrentWindowsUser();
                UserIdTextBox.Text = currentUser;
                
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Auto-filled Windows user: {currentUser}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Failed to get Windows user: {ex.Message}");
                UserIdTextBox.Text = Environment.UserName; // Fallback
            }
            #endif

            // Support Enter key for login
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

            // Auto focus to password field (username is pre-filled)
            this.Loaded += (s, e) =>
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Window loaded, focusing PasswordBox (username pre-filled: admin01)");
                #else
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Window loaded, focusing PasswordBox");
                #endif
                PasswordBox.Focus();
            };

            // Monitor window closing event
            this.Closing += (s, e) =>
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Window closing - LoginSuccessful: {LoginSuccessful}, DialogResult: {DialogResult}");
                #endif
            };
        }

        private async void LoginButton_Click(object? sender, RoutedEventArgs? e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] LoginButton_Click called");
            #endif

            // 清除錯誤訊息
            ErrorPanel.Visibility = Visibility.Collapsed;
            
            // 顯示載入提示
            ShowLoading(true);

            // 獲取輸入
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoginDialog] Attempting login for user: {userId}");
            #endif

            // 驗證輸入
            if (string.IsNullOrWhiteSpace(userId))
            {
                ShowLoading(false);
                ShowError("請輸入帳號 (Please enter User ID)");
                UserIdTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowLoading(false);
                ShowError("請輸入密碼 (Please enter Password)");
                PasswordBox.Focus();
                return;
            }

            // 使用非同步的登入邏輯（避免 UI 凍結）
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Calling SecurityContext.Login...");
                #endif

                // 延遲一點時間讓載入動畫可見（至少顯示 500ms）
                var loginTask = System.Threading.Tasks.Task.Run(() => SecurityContext.Login(userId, password));
                var delayTask = System.Threading.Tasks.Task.Delay(500);
                
                await System.Threading.Tasks.Task.WhenAll(loginTask, delayTask);
                bool success = loginTask.Result;

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login result: {success}");
                #endif

                ShowLoading(false);

                if (success)
                {
                    // 顯示登入成功訊息
                    var user = SecurityContext.CurrentSession.CurrentUser;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login successful for: {user?.DisplayName} ({user?.AccessLevel})");
                    #endif

                    ComplianceContext.LogSystem(
                        $"[LoginDialog] Login successful: {user?.DisplayName} ({user?.AccessLevel})",
                        LogLevel.Success,
                        showInUi: false
                    );

                    LoginSuccessful = true;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LoginDialog] Setting DialogResult = true and closing");
                    #endif
                    
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LoginDialog] Login failed");
                    #endif

                    // 登入失敗，顯示詳細的錯誤訊息
                    ShowError($"登入失敗 Login Failed\n" +
                             $"帳號: {userId}\n\n" +
                             $"可能原因:\n" +
                             $"? Windows 密碼錯誤\n" +
                             $"? 未加入所需 App_ 群組\n" +
                             $"   (需要: App_Operators, App_Instructors, App_Supervisors, 或 App_Admins)\n\n" +
                             $"請確認:\n" +
                             $"1. 使用 Windows 系統帳號密碼\n" +
                             $"2. 帳號已加入上述群組之一\n" +
                             $"3. 檢查 login_debug.log 了解詳情");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Stack trace: {ex.StackTrace}");
                #endif

                ShowLoading(false);
                ShowError($"登入錯誤 Login Error:\n{ex.Message}");
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs? e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] CancelButton_Click called");
            #endif

            LoginSuccessful = false;
            this.DialogResult = false;
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] User cancelled login, closing dialog");
            #endif
            
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
        /// 顯示或隱藏載入提示
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (show)
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                
                // 禁用按鈕避免重複點擊
                UserIdTextBox.IsEnabled = false;
                PasswordBox.IsEnabled = false;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Showing loading indicator");
                #endif
            }
            else
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                // 重新啟用輸入欄位和按鈕
                UserIdTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Hiding loading indicator");
                #endif
            }
        }

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] ForgotPasswordButton_Click called");
            #endif

            // Display password reset instructions
            string message = "Password Reset Instructions\n\n" +
                           "This system uses Windows AD (Active Directory) authentication\n\n" +
                           "To reset your password:\n" +
                           "1. Contact IT support or system administrator\n" +
                           "2. Provide your Windows account name\n" +
                           "3. Follow the password reset procedure\n\n" +
                           "Development/Testing:\n" +
                           "- Username: admin01\n" +
                           "- Password: admin123\n\n" +
                           "Note: Your password is the same as your Windows account";

            MessageBox.Show(
                message,
                "Forgot Password Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            ComplianceContext.LogSystem(
                $"[LoginDialog] User clicked 'Forgot Password' - {UserIdTextBox.Text}",
                LogLevel.Info,
                showInUi: false
            );
        }
        
        /// <summary>
        /// 防止點擊遮罩時關閉對話框
        /// </summary>
        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 不執行任何操作 - 防止點擊遮罩時關閉對話框
            e.Handled = true;
        }
        
        /// <summary>
        /// 顯示登入對話視窗
        /// </summary>
        /// <returns>是否登入成功</returns>
        public static bool ShowLoginDialog()
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog.ShowLoginDialog] Creating dialog...");
            #endif

            try
            {
                var dialog = new LoginDialog();
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog.ShowLoginDialog] Showing dialog...");
                #endif

                bool? result = dialog.ShowDialog();
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog.ShowLoginDialog] Dialog closed - Result: {result}, LoginSuccessful: {dialog.LoginSuccessful}");
                #endif

                return result == true && dialog.LoginSuccessful;
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog.ShowLoginDialog] EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginDialog.ShowLoginDialog] Stack trace: {ex.StackTrace}");
                #endif
                
                MessageBox.Show(
                    $"登入對話框發生錯誤 Login Dialog Error:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                return false;
            }
        }
    }
}
