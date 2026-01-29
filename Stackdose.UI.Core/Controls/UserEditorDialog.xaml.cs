using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者編輯對話視窗 - SQLite 版本
    /// </summary>
    public partial class UserEditorDialog : Window
    {
        private readonly AccessLevel _operatorAccessLevel;
        private readonly UserAccount? _editingUser;
        private readonly bool _isEditMode;

        #region Public Properties

        public string UserId => UserIdTextBox.Text.Trim();
        public string DisplayName => DisplayNameTextBox.Text.Trim();
        public string Password => PasswordBox.Password;
        public string Email => EmailTextBox.Text.Trim();
        public string Department => DepartmentTextBox.Text.Trim();
        public string Remarks => RemarksTextBox.Text.Trim();
        public AccessLevel SelectedAccessLevel => (AccessLevel)(AccessLevelComboBox.SelectedItem ?? AccessLevel.Operator);

        #endregion

        #region Constructors

        /// <summary>
        /// 新增使用者模式
        /// </summary>
        public UserEditorDialog(UserAccount? user, AccessLevel operatorAccessLevel)
        {
            InitializeComponent();

            _operatorAccessLevel = operatorAccessLevel;
            _editingUser = user;
            _isEditMode = user != null;

            InitializeForm();
        }

        #endregion

        #region Initialization

        private void InitializeForm()
        {
            // 設定可選擇的權限等級
            var availableLevels = new List<AccessLevel>();

            // 根據當前使用者權限決定可設定的權限
            if (_operatorAccessLevel == AccessLevel.Admin)
            {
                availableLevels.Add(AccessLevel.Operator);
                availableLevels.Add(AccessLevel.Instructor);
                availableLevels.Add(AccessLevel.Supervisor);
                availableLevels.Add(AccessLevel.Admin);
            }
            else if (_operatorAccessLevel == AccessLevel.Supervisor)
            {
                availableLevels.Add(AccessLevel.Operator);
                availableLevels.Add(AccessLevel.Instructor);
                availableLevels.Add(AccessLevel.Supervisor);
            }
            else
            {
                availableLevels.Add(AccessLevel.Operator);
            }

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
                ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                TitleText.Text = "新增使用者";
            }
        }

        #endregion

        #region Event Handlers

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var userId = UserIdTextBox.Text.Trim();
                var displayName = DisplayNameTextBox.Text.Trim();

                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(userId))
                {
                    CyberMessageBox.Show("請輸入使用者名稱", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    UserIdTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    CyberMessageBox.Show("請輸入顯示名稱", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DisplayNameTextBox.Focus();
                    return;
                }

                // 新增模式需要驗證密碼
                if (!_isEditMode)
                {
                    var password = PasswordBox.Password;
                    var confirmPassword = ConfirmPasswordBox.Password;

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        CyberMessageBox.Show("請輸入密碼", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PasswordBox.Focus();
                        return;
                    }

                    if (password.Length < 6)
                    {
                        CyberMessageBox.Show("密碼長度至少需要 6 個字元", "驗證錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                }

                // 驗證通過
                DialogResult = true;
                Close();
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

        #endregion
    }
}
