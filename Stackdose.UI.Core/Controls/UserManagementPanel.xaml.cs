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
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 使用者管理面板 - 現代化卡片風格 + SQLite 資料庫
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
            
            // 設計模式不執行初始化
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            
            DataContext = this;

            // 使用 SQLite UserManagementService
            _userService = new UserManagementService();

            // 監聽可見性變更
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
                // 取得當前使用者資訊
                var session = SecurityContext.CurrentSession;
                if (!session.IsLoggedIn)
                {
                    CyberMessageBox.Show("請先登入系統", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentUserName = session.CurrentUserName;
                _currentLevel = session.CurrentLevel;
                
                // ?? 修復：從資料庫中查找真實的 UserId
                try
                {
                    var allUsers = await _userService.GetAllUsersAsync();
                    var currentUser = allUsers.FirstOrDefault(u => u.UserId == _currentUserName);
                    
                    if (currentUser != null)
                    {
                        _currentUserId = currentUser.Id;
                        System.Diagnostics.Debug.WriteLine($"[UserManagementPanel] 找到當前使用者: {_currentUserName}, Id={_currentUserId}");
                    }
                    else
                    {
                        // ?? 如果資料庫中找不到，使用 admin01 作為預設創建者
                        var admin = allUsers.FirstOrDefault(u => u.UserId == "admin01");
                        if (admin != null)
                        {
                            _currentUserId = admin.Id;
                            System.Diagnostics.Debug.WriteLine($"[UserManagementPanel] 當前使用者不在資料庫中，使用 admin01 作為創建者, Id={_currentUserId}");
                            ComplianceContext.LogSystem(
                                $"[UserManagement] 當前 AD 使用者 '{_currentUserName}' 不在本地資料庫，使用 admin01 (Id={_currentUserId}) 作為創建者",
                                LogLevel.Warning,
                                showInUi: true
                            );
                        }
                        else
                        {
                            CyberMessageBox.Show(
                                "錯誤：找不到有效的創建者帳號\n請先確認 admin01 帳號存在",
                                "錯誤",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UserManagementPanel] 查找當前使用者失敗: {ex.Message}");
                    CyberMessageBox.Show($"初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                SubtitleText.Text = $"Current: {_currentUserName} ({_currentLevel})";

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
                var users = await _userService.GetManagedUsersAsync(_currentUserId);
                
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                // 更新統計
                TotalUsersText.Text = users.Count.ToString();
                ActiveUsersText.Text = users.Count(u => u.IsActive).ToString();

                ComplianceContext.LogSystem($"載入使用者列表: {users.Count} 位", LogLevel.Info, showInUi: false);
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"載入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                ComplianceContext.LogSystem($"UserManagementPanel LoadUsers Error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 卡片點擊事件
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
                // 開啟新增使用者對話視窗
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
                        CyberMessageBox.Show("使用者建立成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        ComplianceContext.LogSystem($"新增使用者: {dialog.DisplayName}", LogLevel.Success);
                    }
                    else
                    {
                        CyberMessageBox.Show(result.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"新增失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 從 Tag 取得使用者
                UserAccount? targetUser = null;
                if (sender is Button button && button.Tag is UserAccount user)
                {
                    targetUser = user;
                }
                else if (SelectedUser != null)
                {
                    targetUser = SelectedUser;
                }

                if (targetUser == null) return;

                // 開啟編輯對話視窗
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
                        CyberMessageBox.Show("使用者資料已更新", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        ComplianceContext.LogSystem($"編輯使用者: {targetUser.DisplayName}", LogLevel.Success);
                    }
                    else
                    {
                        CyberMessageBox.Show(result.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"編輯失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 從 Tag 取得使用者
                UserAccount? targetUser = null;
                if (sender is Button button && button.Tag is UserAccount user)
                {
                    targetUser = user;
                }
                else if (SelectedUser != null)
                {
                    targetUser = SelectedUser;
                }

                if (targetUser == null) return;

                // 輸入新密碼
                var inputDialog = new InputDialog(
                    "重設密碼",
                    $"請輸入 {targetUser.DisplayName} 的新密碼:")
                {
                    Owner = Window.GetWindow(this)
                };

                if (inputDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(inputDialog.InputText))
                    return;

                var newPassword = inputDialog.InputText;

                // 確認對話視窗
                var result = CyberMessageBox.Show(
                    $"確定要重設 {targetUser.DisplayName} 的密碼嗎？",
                    "確認重設",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // 呼叫服務
                var operationResult = await _userService.ResetPasswordAsync(
                    targetUser.Id,
                    _currentUserId,
                    newPassword
                );

                if (operationResult.Success)
                {
                    CyberMessageBox.Show("密碼重設成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ComplianceContext.LogSystem($"重設密碼: {targetUser.DisplayName}", LogLevel.Success);
                }
                else
                {
                    CyberMessageBox.Show(operationResult.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"重設失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 從 Tag 取得使用者
                UserAccount? targetUser = null;
                if (sender is Button button && button.Tag is UserAccount user)
                {
                    targetUser = user;
                }
                else if (SelectedUser != null)
                {
                    targetUser = SelectedUser;
                }

                if (targetUser == null) return;

                // 不能停用自己
                if (targetUser.Id == _currentUserId)
                {
                    CyberMessageBox.Show("無法停用自己的帳號", "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // 呼叫服務
                var operationResult = targetUser.IsActive
                    ? await _userService.SoftDeleteUserAsync(targetUser.Id, _currentUserId)
                    : await _userService.ActivateUserAsync(targetUser.Id, _currentUserId);

                if (operationResult.Success)
                {
                    await LoadUsersAsync();
                    CyberMessageBox.Show($"{action}成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ComplianceContext.LogSystem($"{action}使用者: {targetUser.DisplayName}", LogLevel.Success);
                }
                else
                {
                    CyberMessageBox.Show(operationResult.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                CyberMessageBox.Show($"操作失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
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
