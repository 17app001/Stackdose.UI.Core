using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Stackdose.UI.Core.Services;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// 群組管理對話視窗
    /// </summary>
    public partial class GroupManagementDialog : Window
    {
        private readonly WindowsAccountService _accountService;
        private readonly WindowsAccountService.UserInfo _userInfo;
        private readonly List<GroupItem> _groups = new List<GroupItem>();

        public class GroupItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            public string GroupName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool WasInitiallySelected { get; set; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public GroupManagementDialog(WindowsAccountService accountService, WindowsAccountService.UserInfo userInfo)
        {
            InitializeComponent();

            _accountService = accountService;
            _userInfo = userInfo;

            InitializeForm();
        }

        private void InitializeForm()
        {
            TitleText.Text = $"群組管理 - {_userInfo.DisplayName}";

            // ?? App_ 群組清單
            var appGroups = new Dictionary<string, string>
            {
                ["App_Operators"] = "App_Operators (操作員)",
                ["App_Instructors"] = "App_Instructors (指導員)",
                ["App_Supervisors"] = "App_Supervisors (主管)",
                ["App_Admins"] = "App_Admins (管理員)"
            };

            foreach (var group in appGroups)
            {
                var isSelected = _userInfo.Groups.Contains(group.Key);
                _groups.Add(new GroupItem
                {
                    GroupName = group.Key,
                    DisplayName = group.Value,
                    IsSelected = isSelected,
                    WasInitiallySelected = isSelected
                });
            }

            GroupListView.ItemsSource = _groups;
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var errors = new List<string>();

                // ?? 處理新增/移除群組
                foreach (var group in _groups)
                {
                    if (group.IsSelected && !group.WasInitiallySelected)
                    {
                        // 加入群組
                        var result = await System.Threading.Tasks.Task.Run(() => 
                            _accountService.AddUserToGroup(_userInfo.SamAccountName, group.GroupName));

                        if (!result.Success)
                        {
                            errors.Add($"加入 {group.DisplayName} 失敗: {result.Message}");
                        }
                    }
                    else if (!group.IsSelected && group.WasInitiallySelected)
                    {
                        // 移除群組
                        var result = await System.Threading.Tasks.Task.Run(() => 
                            _accountService.RemoveUserFromGroup(_userInfo.SamAccountName, group.GroupName));

                        if (!result.Success)
                        {
                            errors.Add($"移除 {group.DisplayName} 失敗: {result.Message}");
                        }
                    }
                }

                if (errors.Count > 0)
                {
                    CyberMessageBox.Show(
                        $"部分操作失敗:\n\n{string.Join("\n", errors)}",
                        "警告",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

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
    }
}
