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
            // ?? DEBUG 模式：自動填入 admin01（測試用）
            System.Diagnostics.Debug.WriteLine("[LoginDialog] DEBUG Mode: Auto-filling admin01 credentials");
            UserIdTextBox.Text = "admin01";
            // 注意：密碼不預填，需手動輸入（安全考量）
            System.Diagnostics.Debug.WriteLine("[LoginDialog] Tip: Use Windows password 'admin01admin01' to login");
            #else
            // ?? RELEASE 模式：自動填入當前 Windows 使用者名稱
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

            // ?? 自動 focus 到密碼欄（因為帳號已預填）
            this.Loaded += (s, e) =>
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Window loaded, focusing PasswordBox (username pre-filled: admin01)");
                #else
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Window loaded, focusing PasswordBox");
                #endif
                PasswordBox.Focus();
            };

            // 監聽視窗關閉事件
            this.Closing += (s, e) =>
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Window closing - LoginSuccessful: {LoginSuccessful}, DialogResult: {DialogResult}");
                #endif
            };
        }

        private void LoginButton_Click(object? sender, RoutedEventArgs? e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] LoginButton_Click called");
            #endif

            // 清除錯誤訊息
            ErrorPanel.Visibility = Visibility.Collapsed;

            // 取得輸入
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordBox.Password;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoginDialog] Attempting login for user: {userId}");
            #endif

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

            // ?? 使用整合後的登入邏輯（支援 AD 驗證）
            try
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Calling SecurityContext.Login...");
                #endif

                bool success = SecurityContext.Login(userId, password);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login result: {success}");
                #endif

                if (success)
                {
                    // ?? 顯示登入成功訊息
                    var user = SecurityContext.CurrentSession.CurrentUser;
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login successful for: {user?.DisplayName} ({user?.AccessLevel})");
                    #endif

                    ComplianceContext.LogSystem(
                        $"[LoginDialog] Login successful: {user?.DisplayName} ({user?.AccessLevel})",
                        Models.LogLevel.Success,
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

                    // ?? 登入失敗，顯示詳細的錯誤訊息
                    ShowError($"登入失敗 Login Failed\n" +
                             $"帳號: {userId}\n\n" +
                             $"可能原因:\n" +
                             $"? Windows 密碼錯誤\n" +
                             $"? 不屬於任何 App_ 群組\n" +
                             $"   (需要: App_Operators, App_Instructors, App_Supervisors, 或 App_Admins)\n\n" +
                             $"請確認:\n" +
                             $"1. 使用 Windows 本機帳號密碼\n" +
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

        private void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            #if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] ForgotPasswordButton_Click called");
            #endif

            // 顯示密碼重置說明
            string message = "密碼重置說明\n\n" +
                           "此系統使用 Windows AD (Active Directory) 驗證\n\n" +
                           "密碼重置步驟：\n" +
                           "1. 聯繫 IT 部門或系統管理員\n" +
                           "2. 提供您的 Windows 帳戶名稱\n" +
                           "3. 遵循公司密碼重置流程\n\n" +
                           "開發/測試環境：\n" +
                           "? 使用者名稱：admin01\n" +
                           "? 密碼：admin01admin01\n\n" +
                           "注意：實際密碼取決於您的 Windows 域帳戶";

            MessageBox.Show(
                message,
                "忘記密碼說明",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            ComplianceContext.LogSystem(
                $"[LoginDialog] User clicked 'Forgot Password' - {UserIdTextBox.Text}",
                Models.LogLevel.Info,
                showInUi: false
            );
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
