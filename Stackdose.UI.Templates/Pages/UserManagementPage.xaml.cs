using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Pages
{
    /// <summary>
    /// UserManagementPage - 使用者管理頁面 (Template 外框)
    /// </summary>
    /// <remarks>
    /// 使用 Stackdose.UI.Core 的 UserManagementPanel 作為核心組件
    /// 提供統一的 Template 風格外框
    /// </remarks>
    public partial class UserManagementPage : UserControl
    {
        public UserManagementPage()
        {
            InitializeComponent();
            
            // 載入完成後更新統計資訊
            this.Loaded += UserManagementPage_Loaded;
        }

        private void UserManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 可以從 UserManagementPanel 取得統計資訊
            UpdateStatistics();
        }

        /// <summary>
        /// 更新統計資訊顯示
        /// </summary>
        private void UpdateStatistics()
        {
            // 可以從 UserManagementPanel 的 DataContext 取得使用者數量
            // 這裡先保持簡單
            TotalUsersText.Text = "User Management System";
        }
    }
}
