using System;
using System.Linq;
using System.Threading.Tasks;
using Stackdose.UI.Core.Services;

namespace PasswordResetTool
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" StackDose 密碼重置工具");
            Console.WriteLine("========================================");
            Console.WriteLine();

            try
            {
                // 建立 UserManagementService
                var service = new UserManagementService();
                
                // 列出所有使用者
                var users = await service.GetAllUsersAsync();
                
                Console.WriteLine("資料庫中的使用者清單:");
                Console.WriteLine(new string('-', 80));
                Console.WriteLine($"{"UserId",-15} {"DisplayName",-20} {"AccessLevel",-15} {"Active"}");
                Console.WriteLine(new string('-', 80));
                
                foreach (var user in users)
                {
                    Console.WriteLine($"{user.UserId,-15} {user.DisplayName,-20} {user.AccessLevel,-15} {(user.IsActive ? "?" : "?")}");
                }
                
                Console.WriteLine();
                
                // 尋找 SuperAdmin
                var superAdmin = users.FirstOrDefault(u => u.UserId == "UID-000001");
                
                if (superAdmin == null)
                {
                    Console.WriteLine("? 錯誤: 找不到 SuperAdmin 帳號 (UID-000001)");
                    Console.WriteLine("   建議: 刪除 StackDoseData.db 並重新啟動程式");
                    Console.ReadKey();
                    return;
                }
                
                Console.WriteLine($"? 找到 SuperAdmin 帳號:");
                Console.WriteLine($"   UserId: {superAdmin.UserId}");
                Console.WriteLine($"   DisplayName: {superAdmin.DisplayName}");
                Console.WriteLine($"   AccessLevel: {superAdmin.AccessLevel}");
                Console.WriteLine();
                
                // 詢問新密碼
                Console.Write("請輸入新密碼 (留空則設為 'admin123'): ");
                string? newPassword = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    newPassword = "admin123";
                }
                
                Console.WriteLine();
                Console.Write($"確認要將密碼重置為 '{newPassword}'? (Y/N): ");
                string? confirm = Console.ReadLine();
                
                if (confirm?.ToUpper() != "Y")
                {
                    Console.WriteLine("? 已取消操作");
                    Console.ReadKey();
                    return;
                }
                
                // 執行密碼重置
                Console.WriteLine();
                Console.WriteLine("正在重置密碼...");
                
                var result = await service.ResetPasswordAsync(
                    targetUserId: superAdmin.Id,
                    operatorUserId: superAdmin.Id, // 自己重置自己
                    newPassword: newPassword
                );
                
                Console.WriteLine();
                
                if (result.Success)
                {
                    Console.WriteLine("========================================");
                    Console.WriteLine(" ? 密碼重置成功!");
                    Console.WriteLine("========================================");
                    Console.WriteLine($" 帳號: {superAdmin.UserId}");
                    Console.WriteLine($" 名稱: {superAdmin.DisplayName}");
                    Console.WriteLine($" 新密碼: {newPassword}");
                    Console.WriteLine("========================================");
                    Console.WriteLine();
                    Console.WriteLine("請使用以上資訊登入系統");
                }
                else
                {
                    Console.WriteLine($"? 密碼重置失敗: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine(" ? 發生錯誤");
                Console.WriteLine("========================================");
                Console.WriteLine($" 錯誤訊息: {ex.Message}");
                Console.WriteLine($" 堆疊追蹤: {ex.StackTrace}");
                Console.WriteLine("========================================");
            }
            
            Console.WriteLine();
            Console.WriteLine("按任意鍵結束...");
            Console.ReadKey();
        }
    }
}
