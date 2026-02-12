using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stackdose.Abstractions.Logging;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Helpers.UI;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// User management panel backed by SQLite.
    /// </summary>
    public partial class UserManagementPanel : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private readonly IUserManagementService _userService;
        private ObservableCollection<UserAccount> _users = new();
        private UserAccount? _selectedUser;
        private int _currentUserId;
        private string _currentUserName = "Unknown";
        private AccessLevel _currentLevel = AccessLevel.Guest;

        #endregion

        #region Properties

        public ObservableCollection<UserAccount> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public UserAccount? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructor

        public UserManagementPanel()
        {
            InitializeComponent();

            _userService = new UserManagementService();

            // Skip runtime initialization in design mode.
            if (ControlRuntime.IsDesignMode(this))
            {
                return;
            }

            DataContext = this;
            IsVisibleChanged += UserManagementPanel_IsVisibleChanged;
        }

        #endregion

        #region Initialization

        private async void UserManagementPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            
            if (this.IsVisible)
            {
                var session = SecurityContext.CurrentSession;
                if (!session.IsLoggedIn)
                {
                    ControlRuntime.ShowWarning("請先登入系統");
                    return;
                }

                _currentUserName = session.CurrentUserName;
                _currentLevel = session.CurrentLevel;

                try
                {
                    if (!await TryResolveCurrentUserIdAsync(session))
                    {
                        ControlRuntime.ShowError("錯誤：找不到有效的建立者帳號\n請確認資料庫中存在 SuperAdmin 帳號");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPanel] Resolve current user failed: {ex.Message}");
                    ControlRuntime.ShowError($"初始化失敗: {ex.Message}");
                    return;
                }

                SubtitleText.Text = $"Current: {_currentUserName} ({_currentLevel})";

                await LoadUsersAsync();
            }
        }

        #endregion

        #region Load Users

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetManagedUsersAsync(_currentUserId);
                
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                TotalUsersText.Text = users.Count.ToString();
                ActiveUsersText.Text = users.Count(u => u.IsActive).ToString();

                ComplianceContext.LogSystem($"載入使用者清單: {users.Count} 筆", LogLevel.Info, showInUi: false);
            }
            catch (Exception ex)
            {
                ControlRuntime.ShowError($"載入失敗: {ex.Message}");
                ComplianceContext.LogSystem($"UserManagementPanel LoadUsers Error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles selection from a user card.
        /// </summary>
        private void UserCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is UserAccount user)
            {
                SelectedUser = user;
                
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[UserManagementPanel] Selected: {user.DisplayName}");
                #endif
            }
        }

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UserEditorDialog(null, _currentLevel)
                {
                    Owner = Window.GetWindow(this),
                    Title = "新增使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = await _userService.CreateUserAsync(
                        dialog.UserId,
                        dialog.DisplayName,
                        dialog.Password,
                        dialog.SelectedAccessLevel,
                        _currentUserId,
                        dialog.Email,
                        dialog.Department,
                        dialog.Remarks
                    );

                    if (result.Success)
                    {
                        await LoadUsersAsync();
                        ControlRuntime.ShowInfo("使用者建立成功");
                        ComplianceContext.LogSystem($"新增使用者: {dialog.DisplayName}", LogLevel.Success);
                    }
                    else
                    {
                        ControlRuntime.ShowError(result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ControlRuntime.ShowError($"新增失敗: {ex.Message}");
            }
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetTargetUser(sender, out var targetUser)) return;

                // �}�ҽs���ܵ���
                var dialog = new UserEditorDialog(targetUser, _currentLevel)
                {
                    Owner = Window.GetWindow(this),
                    Title = $"編輯使用者 - {targetUser.DisplayName}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = await _userService.UpdateUserAsync(
                        targetUser.Id,
                        _currentUserId,
                        dialog.DisplayName,
                        dialog.Email,
                        dialog.Department,
                        dialog.Remarks,
                        dialog.SelectedAccessLevel
                    );

                    if (result.Success)
                    {
                        await LoadUsersAsync();
                        ControlRuntime.ShowInfo("使用者資料已更新");
                        ComplianceContext.LogSystem($"編輯使用者: {targetUser.DisplayName}", LogLevel.Success);
                    }
                    else
                    {
                        ControlRuntime.ShowError(result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                ControlRuntime.ShowError($"編輯失敗: {ex.Message}");
            }
        }

        private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetTargetUser(sender, out var targetUser)) return;

                var inputDialog = new InputDialog(
                    "重設密碼",
                    $"請輸入 {targetUser.DisplayName} 的新密碼:")
                {
                    Owner = Window.GetWindow(this)
                };

                if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.InputText))
                    return;

                var newPassword = inputDialog.InputText;

                var result = CyberMessageBox.Show(
                    $"確定要重設 {targetUser.DisplayName} 的密碼嗎？",
                    "確認重設",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var operationResult = await _userService.ResetPasswordAsync(
                    targetUser.Id,
                    _currentUserId,
                    newPassword
                );

                if (operationResult.Success)
                {
                    ControlRuntime.ShowInfo("密碼重設成功");
                    ComplianceContext.LogSystem($"重設密碼: {targetUser.DisplayName}", LogLevel.Success);
                }
                else
                {
                    ControlRuntime.ShowError(operationResult.Message);
                }
            }
            catch (Exception ex)
            {
                ControlRuntime.ShowError($"重設失敗: {ex.Message}");
            }
        }

        private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!TryGetTargetUser(sender, out var targetUser)) return;

                if (targetUser.Id == _currentUserId)
                {
                    ControlRuntime.ShowWarning("無法停用自己的帳號");
                    return;
                }

                var action = targetUser.IsActive ? "停用" : "啟用";
                var result = CyberMessageBox.Show(
                    $"確定要{action}使用者 {targetUser.DisplayName} 嗎？",
                    $"確認{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var operationResult = targetUser.IsActive
                    ? await _userService.SoftDeleteUserAsync(targetUser.Id, _currentUserId)
                    : await _userService.ActivateUserAsync(targetUser.Id, _currentUserId);

                if (operationResult.Success)
                {
                    await LoadUsersAsync();
                    ControlRuntime.ShowInfo($"{action}成功");
                    ComplianceContext.LogSystem($"{action}使用者: {targetUser.DisplayName}", LogLevel.Success);
                }
                else
                {
                    ControlRuntime.ShowError(operationResult.Message);
                }
            }
            catch (Exception ex)
            {
                ControlRuntime.ShowError($"操作失敗: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private bool TryGetTargetUser(object sender, out UserAccount targetUser)
        {
            if (sender is Button button && button.Tag is UserAccount taggedUser)
            {
                targetUser = taggedUser;
                return true;
            }

            if (SelectedUser != null)
            {
                targetUser = SelectedUser;
                return true;
            }

            targetUser = null!;
            return false;
        }

        private async Task<bool> TryResolveCurrentUserIdAsync(UserSession session)
        {
            var currentUser = session.CurrentUser;
            if (currentUser != null && currentUser.Id > 0)
            {
                _currentUserId = currentUser.Id;
                return true;
            }

            var allUsers = await _userService.GetAllUsersAsync();
            var dbUser = allUsers.FirstOrDefault(u =>
                u.UserId == currentUser?.UserId ||
                u.DisplayName == _currentUserName ||
                u.UserId == _currentUserName);

            if (dbUser != null)
            {
                _currentUserId = dbUser.Id;
                return true;
            }

            var superAdmin = allUsers.FirstOrDefault(u => u.UserId == "UID-000001");
            if (superAdmin == null)
            {
                return false;
            }

            _currentUserId = superAdmin.Id;
            return true;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
