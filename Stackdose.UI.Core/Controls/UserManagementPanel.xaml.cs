using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Stackdose.Abstractions.Logging;
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

        private readonly WindowsAccountService _windowsAccountService = null!;
        private ObservableCollection<WindowsAccountService.UserInfo> _users = new();
        private WindowsAccountService.UserInfo? _selectedUser;
        private bool _hasLoaded = false;

        #endregion

        #region Properties

        public ObservableCollection<WindowsAccountService.UserInfo> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
            }
        }

        public WindowsAccountService.UserInfo? SelectedUser
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

            // ?? 使用 Windows AD 服務
            _windowsAccountService = new WindowsAccountService(System.DirectoryServices.AccountManagement.ContextType.Machine);

            // ?? 監聽 IsVisibleChanged 事件
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

        private async Task LoadUsersAsync()
        {
            try
            {
                var session = SecurityContext.CurrentSession;
                if (!session.IsLoggedIn)
                {
                    CyberMessageBox.Show("請先登入系統", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ?? 檢查是否有管理員權限
                if (!WindowsAccountService.IsRunningAsAdministrator())
                {
                    CyberMessageBox.Show(
                        "?? 警告：程式未以管理員權限執行\n\n" +
                        "大部分使用者管理功能需要管理員權限。\n" +
                        "建議以管理員身分重新執行程式。",
                        "權限提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }

                // ?? 從 Windows AD 載入使用者
                var users = await System.Threading.Tasks.Task.Run(() => _windowsAccountService.ListAllAppUsers());
                
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                ComplianceContext.LogSystem($"載入 Windows 使用者清單: {users.Count} 位", LogLevel.Info);
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
                var loggedInUser = session.CurrentUser;

                if (loggedInUser == null)
                {
                    CyberMessageBox.Show("找不到當前使用者資訊", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ?? 開啟新增 Windows 使用者對話視窗
                var dialog = new UserEditorDialog(_windowsAccountService, loggedInUser.AccessLevel)
                {
                    Owner = Window.GetWindow(this),
                    Title = "新增 Windows 使用者"
                };

                if (dialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show("使用者建立成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // ?? Windows AD 使用者管理：變更群組
                var groupDialog = new GroupManagementDialog(_windowsAccountService, SelectedUser)
                {
                    Owner = Window.GetWindow(this),
                    Title = $"群組管理 - {SelectedUser.SamAccountName}"
                };

                if (groupDialog.ShowDialog() == true)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show("群組設定已更新", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
                // ?? 輸入新密碼
                var inputDialog = new InputDialog(
                    "重設密碼",
                    $"請輸入 {SelectedUser.DisplayName} 的新密碼:\n\n?? 此操作需要管理員權限")
                {
                    Owner = Window.GetWindow(this)
                };

                if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.InputText))
                    return;

                var newPassword = inputDialog.InputText;

                // ?? 確認對話視窗
                var result = CyberMessageBox.Show(
                    $"確定要重設 {SelectedUser.DisplayName} 的密碼嗎？",
                    "確認重設",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // ?? 呼叫 Windows AD API
                var operationResult = await System.Threading.Tasks.Task.Run(() => 
                    _windowsAccountService.ResetPassword(SelectedUser.SamAccountName, newPassword));

                if (operationResult.Success)
                {
                    CyberMessageBox.Show("密碼重設成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ComplianceContext.LogAuditTrail(
                        "Windows 使用者",
                        SelectedUser.SamAccountName,
                        "密碼",
                        "已重設",
                        $"由 {SecurityContext.CurrentSession.CurrentUserName} 執行",
                        showInUi: true
                    );
                }
                else
                {
                    CyberMessageBox.Show($"重設失敗: {operationResult.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // ?? 不能停用自己
                if (SelectedUser.SamAccountName.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    CyberMessageBox.Show("無法停用自己的帳號", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var action = SelectedUser.IsEnabled ? "停用" : "啟用";
                var result = CyberMessageBox.Show(
                    $"確定要{action}使用者 {SelectedUser.DisplayName} 嗎？\n\n?? 此操作需要管理員權限",
                    $"確認{action}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // ?? 呼叫 Windows AD API
                var operationResult = await System.Threading.Tasks.Task.Run(() => 
                    _windowsAccountService.UpdateUserStatus(SelectedUser.SamAccountName, !SelectedUser.IsEnabled));

                if (operationResult.Success)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show($"{action}成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    ComplianceContext.LogAuditTrail(
                        "Windows 使用者",
                        SelectedUser.SamAccountName,
                        SelectedUser.IsEnabled ? "啟用" : "停用",
                        !SelectedUser.IsEnabled ? "啟用" : "停用",
                        $"由 {SecurityContext.CurrentSession.CurrentUserName} 執行",
                        showInUi: true
                    );
                }
                else
                {
                    CyberMessageBox.Show($"{action}失敗: {operationResult.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // ?? 顯示使用者群組資訊
                var groups = SelectedUser.Groups;
                
                var message = groups.Count > 0
                    ? $"使用者: {SelectedUser.DisplayName} ({SelectedUser.SamAccountName})\n\n" +
                      $"狀態: {(SelectedUser.IsEnabled ? "啟用" : "停用")}\n\n" +
                      $"所屬群組:\n" + string.Join("\n", groups.Select(g => $"  ? {g}"))
                    : $"使用者: {SelectedUser.DisplayName}\n\n無群組資訊";

                CyberMessageBox.Show(
                    message,
                    "使用者資訊",
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
                // ?? 修正：使用 IsEnabled 屬性
                ToggleActiveButton.Content = SelectedUser.IsEnabled ? "?? 停用" : "? 啟用";
                ToggleActiveButton.Background = SelectedUser.IsEnabled 
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
