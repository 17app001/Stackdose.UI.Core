using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Services;
using System.Windows;
using System.Windows.Input;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Login dialog for application authentication.
    /// </summary>
    public partial class LoginDialog : Window
    {
        private const int MinimumLoadingDurationMs = 500;

        public bool LoginSuccessful { get; private set; } = false;

        public LoginDialog()
        {
            InitializeComponent();
            InitializeDefaultUserId();
            WireWindowEvents();
        }

        private void InitializeDefaultUserId()
        {

            #if DEBUG
            // DEBUG mode: Auto-fill superadmin (for testing)
            System.Diagnostics.Debug.WriteLine("[LoginDialog] DEBUG Mode: Auto-filling superadmin credentials");
            UserIdTextBox.Text = "UID-000001";
            PasswordBox.Password = "superadmin";  // Pre-fill password for testing
            System.Diagnostics.Debug.WriteLine("[LoginDialog] Tip: Use UID-000001 / superadmin to login");
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
        }

        private void WireWindowEvents()
        {
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
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Window loaded, focusing PasswordBox");
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

            // Clear previous error.
            ErrorPanel.Visibility = Visibility.Collapsed;

            // Show loading state.
            ShowLoading(true);

            var userId = UserIdTextBox.Text.Trim();
            var password = PasswordBox.Password;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoginDialog] Attempting login for user: {userId}");
            #endif

            if (!ValidateCredentials(userId, password))
            {
                ShowLoading(false);
                return;
            }

            try
            {
                var success = await AuthenticateAsync(userId, password);

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login result: {success}");
                #endif

                ShowLoading(false);

                if (success)
                {
                    HandleLoginSuccess();
                }
                else
                {
                    HandleLoginFailure(userId);
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoginDialog] Stack trace: {ex.StackTrace}");
                #endif

                ShowLoading(false);
                ShowError($"登入發生錯誤 (Login Error):\n{ex.Message}");
            }
        }

        private static async Task<bool> AuthenticateAsync(string userId, string password)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] Calling SecurityContext.LoginAsync...");
#endif

            var loginTask = SecurityContext.LoginAsync(userId, password);
            var delayTask = Task.Delay(MinimumLoadingDurationMs);

            await Task.WhenAll(loginTask, delayTask).ConfigureAwait(false);
            return await loginTask.ConfigureAwait(false);
        }

        private bool ValidateCredentials(string userId, string password)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                ShowError("請輸入帳號 (Please enter User ID)");
                UserIdTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("請輸入密碼 (Please enter Password)");
                PasswordBox.Focus();
                return false;
            }

            return true;
        }

        private void HandleLoginSuccess()
        {
            var user = SecurityContext.CurrentSession.CurrentUser;

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LoginDialog] Login successful for: {user?.DisplayName} ({user?.AccessLevel})");
#endif

            ComplianceContext.LogSystem(
                $"[LoginDialog] Login successful: {user?.DisplayName} ({user?.AccessLevel})",
                LogLevel.Success,
                showInUi: false);

            LoginSuccessful = true;
            DialogResult = true;
            Close();
        }

        private void HandleLoginFailure(string userId)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[LoginDialog] Login failed");
#endif

            ShowError($"登入失敗 (Login Failed)\n" +
                     $"帳號: {userId}\n\n" +
                     "可能原因:\n" +
                     "- 帳號或密碼錯誤\n" +
                     "- 帳號已停用\n\n" +
                     "請確認:\n" +
                     "1. 使用正確的帳號與密碼\n" +
                     "2. 帳號狀態仍為啟用\n" +
                     "3. 如需協助請聯絡系統管理員");

            PasswordBox.Clear();
            PasswordBox.Focus();
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
        /// Shows or hides loading UI state.
        /// </summary>
        private void ShowLoading(bool show)
        {
            if (show)
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ErrorPanel.Visibility = Visibility.Collapsed;
                
                // Temporarily disable inputs while logging in.
                UserIdTextBox.IsEnabled = false;
                PasswordBox.IsEnabled = false;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine("[LoginDialog] Showing loading indicator");
                #endif
            }
            else
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                // Re-enable inputs when loading ends.
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
            string message = "密碼重設說明 (Password Reset Instructions)\n\n" +
                           "請聯絡系統管理員協助重設密碼\n\n" +
                           "To reset your password:\n" +
                           "1. Contact system administrator\n" +
                           "2. Provide your User ID\n" +
                           "3. Follow the password reset procedure\n\n" +
                           "開發/測試預設帳號:\n" +
                           "Development/Testing:\n" +
                           "- Username: admin01\n" +
                           "- Password: admin123";

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
        /// Prevent click-through on dialog overlay.
        /// </summary>
        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Block bubbling to underlying window.
            e.Handled = true;
        }
        
        /// <summary>
        /// Shows login dialog and returns auth result.
        /// </summary>
        /// <returns>True if login succeeds.</returns>
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
                    $"登入視窗發生錯誤 (Login Dialog Error):\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                
                return false;
            }
        }
    }
}
