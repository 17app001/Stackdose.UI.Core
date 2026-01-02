using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者管理面板 (符合 FDA 21 CFR Part 11)
    /// </summary>
    public partial class UserManagementPanel : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private readonly IUserManagementService _userService;
        private ObservableCollection<UserAccount> _users = new();
        private UserAccount? _selectedUser;
        private bool _hasLoaded = false; // ?? 新增：追蹤是否已載入過

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
                UpdateButtonStates();
            }
        }

        #endregion

        #region Constructor

        public UserManagementPanel()
        {
            InitializeComponent();
            
            // ?? 設計模式不執行初始化
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            
            DataContext = this;

            _userService = new UserManagementService();

            // ?? 改用 IsVisibleChanged 事件而非 Loaded
            IsVisibleChanged += UserManagementPanel_IsVisibleChanged;
        }

        #endregion

        #region Initialization

        // ?? 移除舊的 Loaded 事件處理，改用 IsVisibleChanged
        private async void UserManagementPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // ?? 設計模式不執行
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            
            // 只有當面板變為可見且尚未載入過時才執行
            if (this.IsVisible && !_hasLoaded)
            {
                _hasLoaded = true;
                
                // 更新標題
                var session = SecurityContext.CurrentSession;
                SubtitleText.Text = $"當前登入: {session.CurrentUserName} ({session.CurrentLevel})";

                // 載入使用者列表
                await LoadUsersAsync();
            }
        }

        #endregion

        #region Load Users

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            try
            {
                var session = SecurityContext.CurrentSession;
                if (!session.IsLoggedIn)
                {
                    CyberMessageBox.Show("請先登入系統", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ?? 修正：根據當前使用者的 UserId（而非 DisplayName）查詢資料庫取得 ID
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 取得可管理的使用者列表
                var users = await _userService.GetManagedUsersAsync(loggedInUser.Id);
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                ComplianceContext.LogSystem($"載入使用者列表: {users.Count} 筆", LogLevel.Info);
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                ComplianceContext.LogSystem($"UserManagementPanel LoadUsers Error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Button Event Handlers

        private async void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var session = SecurityContext.CurrentSession;
                // ?? 修正：直接使用 CurrentUser
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 開啟新增使用者對話框
                var dialog = new UserEditorDialog(_userService, loggedInUser.Id, loggedInUser.AccessLevel)
                {
                    Owner = Window.GetWindow(this),
                    Title = "新增使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show("使用者創建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"新增失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser == null) return;

            try
            {
                var session = SecurityContext.CurrentSession;
                // ?? 修正：直接使用 CurrentUser
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 開啟編輯對話框
                var dialog = new UserEditorDialog(_userService, loggedInUser.Id, loggedInUser.AccessLevel, SelectedUser)
                {
                    Owner = Window.GetWindow(this),
                    Title = $"編輯使用者 - {SelectedUser.UserId}"
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show("使用者資料已更新", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"編輯失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser == null) return;

            try
            {
                // 輸入新密碼
                var inputDialog = new InputDialog(
                    "重設密碼",
                    $"請輸入 {SelectedUser.DisplayName} 的新密碼:")
                {
                    Owner = Window.GetWindow(this)
                };

                if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.InputText))
                    return;

                var newPassword = inputDialog.InputText;

                // 確認對話框
                var result = CyberMessageBox.Show(
                    $"確定要重設 {SelectedUser.DisplayName} 的密碼嗎？",
                    "確認重設",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
                
                var session = SecurityContext.CurrentSession;
                // ?? 修正：直接使用 CurrentUser
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var (success, message) = await _userService.ResetPasswordAsync(
                    SelectedUser.Id,
                    loggedInUser.Id,
                    newPassword);

                if (success)
                {
                    CyberMessageBox.Show("密碼重設成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ComplianceContext.LogSystem($"重設密碼: {SelectedUser.UserId}", LogLevel.Info);
                }
                else
                {
                    CyberMessageBox.Show($"重設失敗: {message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"重設失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser == null) return;

            try
            {
                var session = SecurityContext.CurrentSession;
                // ?? 修正：直接使用 CurrentUser
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 不能刪除自己
                if (SelectedUser.Id == loggedInUser.Id)
                {
                    CyberMessageBox.Show("不能停用自己的帳號", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var action = SelectedUser.IsActive ? "停用" : "啟用";
                var result = CyberMessageBox.Show(
                    $"確定要{action}使用者 {SelectedUser.DisplayName} 嗎？",
                    $"確認{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var (success, message) = SelectedUser.IsActive
                    ? await _userService.SoftDeleteUserAsync(SelectedUser.Id, loggedInUser.Id)
                    : await _userService.ActivateUserAsync(SelectedUser.Id, loggedInUser.Id);

                if (success)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show($"{action}成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    CyberMessageBox.Show($"{action}失敗: {message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"操作失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ViewAuditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser == null) return;

            try
            {
                var logs = await _userService.GetAuditLogsAsync(SelectedUser.Id, 50);
                
                var message = logs.Count > 0
                    ? string.Join("\n", logs.Take(10).Select(l =>
                        $"[{l.Timestamp:yyyy-MM-dd HH:mm}] {l.OperatorUserName} - {l.Action}: {l.Details}"))
                    : "無稽核記錄";

                CyberMessageBox.Show(
                    $"使用者 {SelectedUser.DisplayName} 的稽核記錄 (最近 10 筆):\n\n{message}",
                    "稽核記錄",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"查詢失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        #endregion

        #region Helper Methods

        private void UserDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasSelection = SelectedUser != null;
            
            EditUserButton.IsEnabled = hasSelection;
            ResetPasswordButton.IsEnabled = hasSelection;
            ToggleActiveButton.IsEnabled = hasSelection;
            ViewAuditButton.IsEnabled = hasSelection;

            if (hasSelection && SelectedUser != null)
            {
                ToggleActiveButton.Content = SelectedUser.IsActive ? "? 停用" : "? 啟用";
                ToggleActiveButton.Background = SelectedUser.IsActive 
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54))
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80));
            }
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
