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
        private readonly IUserManagementService _userService;
        private readonly int _operatorUserId;
        private readonly AccessLevel _operatorAccessLevel;
        private readonly UserAccount? _editingUser;
        private readonly bool _isEditMode;

        public UserEditorDialog(
            IUserManagementService userService,
            int operatorUserId,
            AccessLevel operatorAccessLevel,
            UserAccount? editingUser = null)
        {
            InitializeComponent();

            _userService = userService;
            _operatorUserId = operatorUserId;
            _operatorAccessLevel = operatorAccessLevel;
            _editingUser = editingUser;
            _isEditMode = editingUser != null;

            InitializeForm();
        }

        private void InitializeForm()
        {
            // 設定可選的權限等級
            var availableLevels = Enum.GetValues(typeof(AccessLevel))
                .Cast<AccessLevel>()
                .Where(level => _userService.CanManageUser(_operatorAccessLevel, level))
                .ToList();

            AccessLevelComboBox.ItemsSource = availableLevels;
            AccessLevelComboBox.SelectedIndex = 0;

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
                var accessLevel = (AccessLevel)(AccessLevelComboBox.SelectedItem ?? AccessLevel.Operator);
                var email = EmailTextBox.Text.Trim();
                var department = DepartmentTextBox.Text.Trim();
                var remarks = RemarksTextBox.Text.Trim();

                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(userId))
                {
                    CyberMessageBox.Show("請輸入使用者 ID", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    CyberMessageBox.Show("請輸入顯示名稱", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 新增模式
                if (!_isEditMode)
                {
                    var password = PasswordBox.Password;
                    if (string.IsNullOrWhiteSpace(password))
                    {
                        CyberMessageBox.Show("請輸入密碼", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (password.Length < 8)
                    {
                        CyberMessageBox.Show("密碼長度至少需要 8 個字元", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var (success, message, user) = await _userService.CreateUserAsync(
                        userId,
                        displayName,
                        password,
                        accessLevel,
                        _operatorUserId,
                        string.IsNullOrWhiteSpace(email) ? null : email,
                        string.IsNullOrWhiteSpace(department) ? null : department,
                        string.IsNullOrWhiteSpace(remarks) ? null : remarks);

                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        CyberMessageBox.Show($"創建失敗: {message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                // 編輯模式
                else if (_editingUser != null)
                {
                    var (success, message) = await _userService.UpdateUserAsync(
                        _editingUser.Id,
                        _operatorUserId,
                        displayName,
                        string.IsNullOrWhiteSpace(email) ? null : email,
                        string.IsNullOrWhiteSpace(department) ? null : department,
                        string.IsNullOrWhiteSpace(remarks) ? null : remarks,
                        accessLevel);

                    if (success)
                    {
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        CyberMessageBox.Show($"更新失敗: {message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
