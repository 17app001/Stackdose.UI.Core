using System;
using System.Threading.Tasks;
using System.Windows;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Services;
using Stackdose.UI.Core.Helpers;

namespace ModelB.Demo
{
    /// <summary>
    /// UserManager 創建使用者測試程式
    /// </summary>
    public partial class UserManagerTest : Window
    {
        private UserManagementService _userService;

        public UserManagerTest()
        {
            InitializeComponent();
            
            // 初始化測試環境
            InitializeTest();
        }

        private void InitializeTest()
        {
            try
            {
                LogMessage("========================================");
                LogMessage("UserManager 創建測試開始");
                LogMessage("========================================");

                // 1. 初始化 ComplianceContext
                LogMessage("[1/6] 初始化 ComplianceContext...");
                var _ = ComplianceContext.CurrentUser;
                LogMessage("? ComplianceContext 初始化成功");

                // 2. 初始化 UserManagementService
                LogMessage("[2/6] 初始化 UserManagementService...");
                _userService = new UserManagementService();
                LogMessage("? UserManagementService 初始化成功");

                // 3. 檢查預設 Admin 帳號
                LogMessage("[3/6] 檢查預設 Admin 帳號...");
                CheckDefaultAdmin();

                // 4. 測試登入
                LogMessage("[4/6] 測試登入 admin01...");
                TestLogin();

                // 5. 顯示測試按鈕
                LogMessage("[5/6] 顯示測試按鈕...");
                TestButton.IsEnabled = true;
                LogMessage("? 測試環境準備完成");

                LogMessage("========================================");
                LogMessage("?? 請點擊「測試創建使用者」按鈕");
                LogMessage("========================================");
            }
            catch (Exception ex)
            {
                LogMessage($"? 初始化錯誤: {ex.Message}");
                LogMessage($"   Stack: {ex.StackTrace}");
            }
        }

        private async void CheckDefaultAdmin()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                LogMessage($"   資料庫中的使用者數量: {users.Count}");

                var admin = users.FirstOrDefault(u => u.UserId == "admin01");
                if (admin != null)
                {
                    LogMessage($"? 找到預設 Admin 帳號:");
                    LogMessage($"   UserId: {admin.UserId}");
                    LogMessage($"   DisplayName: {admin.DisplayName}");
                    LogMessage($"   AccessLevel: {admin.AccessLevel}");
                    LogMessage($"   IsActive: {admin.IsActive}");
                    LogMessage($"   CreatedBy: {admin.CreatedBy}");
                }
                else
                {
                    LogMessage("? 找不到預設 Admin 帳號 (admin01)");
                    LogMessage("   這可能導致創建使用者失敗！");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"? 檢查 Admin 帳號錯誤: {ex.Message}");
            }
        }

        private async void TestLogin()
        {
            try
            {
                // ?? UserManagementService 沒有 AuthenticateAsync，改用 GetAllUsersAsync 檢查
                var users = await _userService.GetAllUsersAsync();
                var admin01 = users.FirstOrDefault(u => u.UserId == "admin01");
                
                if (admin01 != null)
                {
                    LogMessage($"? 找到 admin01 帳號:");
                    LogMessage($"   UserId: {admin01.UserId}");
                    LogMessage($"   DisplayName: {admin01.DisplayName}");
                    LogMessage($"   AccessLevel: {admin01.AccessLevel}");
                }
                else
                {
                    LogMessage($"? 找不到 admin01 帳號");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"? 檢查 admin01 錯誤: {ex.Message}");
            }
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            TestButton.IsEnabled = false;
            LogMessage("");
            LogMessage("========================================");
            LogMessage("開始測試創建使用者");
            LogMessage("========================================");

            try
            {
                // Step 1: 取得 admin01 的 UserId
                LogMessage("[Step 1] 取得 admin01 使用者資訊...");
                var adminUser = (await _userService.GetAllUsersAsync())
                    .FirstOrDefault(u => u.UserId == "admin01");

                if (adminUser == null)
                {
                    LogMessage("? 錯誤：找不到 admin01 使用者");
                    LogMessage("   解決方法：");
                    LogMessage("   1. 確認 UserManagementService 的 EnsureDefaultAdminExists() 有執行");
                    LogMessage("   2. 檢查資料庫檔案權限");
                    LogMessage("   3. 查看 Debug 輸出中的錯誤訊息");
                    return;
                }

                LogMessage($"? 找到 admin01:");
                LogMessage($"   Id: {adminUser.Id}");
                LogMessage($"   UserId: {adminUser.UserId}");
                LogMessage($"   DisplayName: {adminUser.DisplayName}");

                // Step 2: 創建測試使用者
                LogMessage("");
                LogMessage("[Step 2] 創建測試使用者 test01...");
                var result = await _userService.CreateUserAsync(
                    userId: "test01",
                    displayName: "Test User 01",
                    password: "test123",
                    accessLevel: AccessLevel.Operator,
                    creatorUserId: adminUser.Id, // ?? 使用 admin01 的 Id
                    email: "test01@example.com",
                    department: "測試部門",
                    remarks: "由測試程式建立"
                );

                LogMessage("");
                if (result.Success)
                {
                    LogMessage("========================================");
                    LogMessage("??? 創建成功！???");
                    LogMessage("========================================");
                    LogMessage($"   UserId: {result.User?.UserId}");
                    LogMessage($"   DisplayName: {result.User?.DisplayName}");
                    LogMessage($"   AccessLevel: {result.User?.AccessLevel}");
                    LogMessage($"   Email: {result.User?.Email}");
                    LogMessage($"   Department: {result.User?.Department}");
                    LogMessage($"   CreatedBy: {result.User?.CreatedBy}");
                    LogMessage($"   CreatedAt: {result.User?.CreatedAt}");
                    LogMessage("========================================");

                    // Step 3: 驗證使用者是否真的存在
                    LogMessage("");
                    LogMessage("[Step 3] 驗證使用者是否存在於資料庫...");
                    var allUsers = await _userService.GetAllUsersAsync();
                    var createdUser = allUsers.FirstOrDefault(u => u.UserId == "test01");
                    
                    if (createdUser != null)
                    {
                        LogMessage("? 驗證成功：使用者已存在於資料庫");
                        LogMessage($"   資料庫中的 UserId: {createdUser.UserId}");
                        LogMessage($"   資料庫中的 DisplayName: {createdUser.DisplayName}");
                    }
                    else
                    {
                        LogMessage("? 驗證失敗：使用者不存在於資料庫（這不應該發生！）");
                    }
                }
                else
                {
                    LogMessage("========================================");
                    LogMessage("??? 創建失敗！???");
                    LogMessage("========================================");
                    LogMessage($"   錯誤訊息: {result.Message}");
                    LogMessage("========================================");

                    // 顯示詳細的錯誤分析
                    LogMessage("");
                    LogMessage("?? 錯誤分析:");
                    if (result.Message.Contains("找不到"))
                    {
                        LogMessage("   可能原因：");
                        LogMessage("   1. creatorUserId 無效");
                        LogMessage("   2. admin01 帳號未正確建立");
                        LogMessage("   3. 資料庫連線問題");
                    }
                    else if (result.Message.Contains("已存在"))
                    {
                        LogMessage("   可能原因：");
                        LogMessage("   1. test01 使用者已經存在");
                        LogMessage("   2. 請嘗試使用不同的 UserId");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("========================================");
                LogMessage("? 測試過程發生例外錯誤");
                LogMessage("========================================");
                LogMessage($"錯誤訊息: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    LogMessage("");
                    LogMessage("內部錯誤:");
                    LogMessage($"   {ex.InnerException.Message}");
                    LogMessage($"   {ex.InnerException.StackTrace}");
                }
            }
            finally
            {
                LogMessage("");
                LogMessage("========================================");
                LogMessage("測試完成");
                LogMessage("========================================");
                TestButton.IsEnabled = true;
            }
        }

        private void LogMessage(string message)
        {
            OutputTextBox.AppendText(message + "\n");
            OutputTextBox.ScrollToEnd();
            System.Diagnostics.Debug.WriteLine($"[UserManagerTest] {message}");
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
