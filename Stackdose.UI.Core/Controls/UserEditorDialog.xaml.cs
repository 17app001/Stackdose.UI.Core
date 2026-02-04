using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// User Editor Dialog - SQLite Version
    /// </summary>
    public partial class UserEditorDialog : Window
    {
        private readonly AccessLevel _operatorAccessLevel;
        private readonly UserAccount? _editingUser;
        private readonly bool _isEditMode;
        private readonly string _generatedUserId;

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
        /// Create/Edit User Mode
        /// </summary>
        public UserEditorDialog(UserAccount? user, AccessLevel operatorAccessLevel)
        {
            InitializeComponent();

            _operatorAccessLevel = operatorAccessLevel;
            _editingUser = user;
            _isEditMode = user != null;

            // Generate next UID for new user
            if (!_isEditMode)
            {
                try
                {
                    var userService = new UserManagementService();
                    _generatedUserId = userService.GenerateNextUserId();
                }
                catch
                {
                    _generatedUserId = "UID-000001";
                }
            }
            else
            {
                _generatedUserId = user?.UserId ?? "";
            }

            InitializeForm();
        }

        #endregion

        #region Initialization

        private void InitializeForm()
        {
            // Set available access levels
            var availableLevels = new List<AccessLevel>();

            // Determine available levels based on operator's access level
            if (_operatorAccessLevel == AccessLevel.SuperAdmin)
            {
                // SuperAdmin can set all levels
                availableLevels.Add(AccessLevel.Operator);
                availableLevels.Add(AccessLevel.Instructor);
                availableLevels.Add(AccessLevel.Supervisor);
                availableLevels.Add(AccessLevel.Admin);
                availableLevels.Add(AccessLevel.SuperAdmin);
            }
            else if (_operatorAccessLevel == AccessLevel.Admin)
            {
                // Admin can set Operator ~ Admin (not SuperAdmin)
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

            // Edit Mode
            if (_isEditMode && _editingUser != null)
            {
                TitleText.Text = $"Edit User - {_editingUser.UserId}";
                UserIdTextBox.Text = _editingUser.UserId;
                UserIdTextBox.IsReadOnly = true; // Cannot modify UserId
                UserIdTextBox.IsEnabled = false;
                UserIdTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                DisplayNameTextBox.Text = _editingUser.DisplayName;
                AccessLevelComboBox.SelectedItem = _editingUser.AccessLevel;
                EmailTextBox.Text = _editingUser.Email;
                DepartmentTextBox.Text = _editingUser.Department;
                RemarksTextBox.Text = _editingUser.Remarks;

                // Edit mode doesn't need password input
                PasswordLabel.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Create Mode - Auto-generate UID (read-only)
                TitleText.Text = "Create New User";
                UserIdTextBox.Text = _generatedUserId;
                UserIdTextBox.IsReadOnly = true; // ?? UID is auto-generated, cannot modify
                UserIdTextBox.IsEnabled = false;
                UserIdTextBox.Foreground = System.Windows.Media.Brushes.LightGray;
                UserIdTextBox.ToolTip = "User ID is auto-generated and cannot be modified";
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

                // Validate required fields
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    CyberMessageBox.Show("Please enter Display Name", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DisplayNameTextBox.Focus();
                    return;
                }

                // Create mode needs password validation
                if (!_isEditMode)
                {
                    var password = PasswordBox.Password;
                    var confirmPassword = ConfirmPasswordBox.Password;

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        CyberMessageBox.Show("Please enter Password", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PasswordBox.Focus();
                        return;
                    }

                    if (password.Length < 6)
                    {
                        CyberMessageBox.Show("Password must be at least 6 characters", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        PasswordBox.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(confirmPassword))
                    {
                        CyberMessageBox.Show("Please confirm your password", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ConfirmPasswordBox.Focus();
                        return;
                    }

                    if (password != confirmPassword)
                    {
                        CyberMessageBox.Show("Passwords do not match!\nPlease make sure both passwords are the same.", "Password Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ConfirmPasswordBox.Clear();
                        PasswordBox.Focus();
                        return;
                    }
                }

                // Validation passed
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
