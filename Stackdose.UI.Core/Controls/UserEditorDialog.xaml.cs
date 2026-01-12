using System;
using System.Linq;
using System.Windows;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者編輯對話框
    /// </summary>
    public partial class UserEditorDialog : Window
    {
        private readonly WindowsAccountService _windowsAccountService;
        private readonly AccessLevel _operatorAccessLevel;
        private readonly UserAccount? _editingUser;
        private readonly bool _isEditMode;

        public UserEditorDialog(
            WindowsAccountService windowsAccountService,
            AccessLevel operatorAccessLevel)
        {
            InitializeComponent();

            _windowsAccountService = windowsAccountService;
            _operatorAccessLevel = operatorAccessLevel;

            InitializeForm();
        }

        private void InitializeForm()
        {
            // ?? 設定可選擇的群組
            var availableGroups = new List<string>();

            // 根據當前使用者權限決定可建立的群組
            if (_operatorAccessLevel == AccessLevel.Admin)
            {
                availableGroups.Add("App_Operators");
                availableGroups.Add("App_Instructors");
                availableGroups.Add("App_Supervisors");
                availableGroups.Add("App_Admins");
            }
            else if (_operatorAccessLevel == AccessLevel.Supervisor)
            {
                availableGroups.Add("App_Operators");
                availableGroups.Add("App_Instructors");
                availableGroups.Add("App_Supervisors");
            }
            else
            {
                availableGroups.Add("App_Operators");
            }

            AccessLevelComboBox.ItemsSource = availableGroups;
            AccessLevelComboBox.SelectedIndex = 0;

            TitleText.Text = "新增 Windows 使用者";
            UserIdTextBox.IsReadOnly = false;

            // 編輯模式
            if (_isEditMode && _editingUser != null)
            {
                TitleText.Text = $"編輯使用者 - {_editingUser.UserId}";
                UserIdTextBox.Text = _editingUser.UserId;
                UserIdTextBox.IsReadOnly = true; // 不可修改 UserId
                DisplayNameTextBox.Text = _editingUser.DisplayName;
                AccessLevelComboBox.SelectedItem = _editingUser.AccessLevel;
                EmailTextBox.Text = _editingUser.Email;
                DepartmentTextBox.Text = _editingUser.Department;
                RemarksTextBox.Text = _editingUser.Remarks;

                // 編輯模式不需要輸入密碼
                PasswordLabel.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                TitleText.Text = "新增使用者";
            }
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userId = UserIdTextBox.Text.Trim();
                var displayName = DisplayNameTextBox.Text.Trim();
                var selectedGroup = AccessLevelComboBox.SelectedItem?.ToString() ?? "App_Operators";
                var password = PasswordBox.Password;
                var confirmPassword = ConfirmPasswordBox.Password;
                var description = RemarksTextBox.Text.Trim();

                // ?? 驗證必填欄位
                if (string.IsNullOrWhiteSpace(userId))
                {
                    CyberMessageBox.Show("請輸入使用者名稱", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    CyberMessageBox.Show("請輸入顯示名稱", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    CyberMessageBox.Show("請輸入密碼", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (password.Length < 8)
                {
                    CyberMessageBox.Show("密碼長度至少需要 8 個字元", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    CyberMessageBox.Show("請確認您的密碼", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                if (password != confirmPassword)
                {
                    CyberMessageBox.Show("密碼不符合！\n請確保兩次密碼輸入的內容完全相同。", "密碼不一致", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Clear();
                    PasswordBox.Focus();
                    return;
                }

                // ?? 呼叫 Windows AD API 建立使用者
                var result = await System.Threading.Tasks.Task.Run(() => 
                    _windowsAccountService.CreateUser(userId, password, displayName, selectedGroup, description));

                if (result.Success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    CyberMessageBox.Show($"建立使用者失敗: {result.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"操作失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
