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
           
        }      
    }
}
