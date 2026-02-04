# 密碼問題修正指南

## 問題分析

根據程式碼檢查，發現密碼無法登入的可能原因：

### 1. **預設 SuperAdmin 帳號資訊**
- **UserId**: `UID-000001`
- **DisplayName**: `SuperAdmin`
- **預設密碼**: `superadmin`
- 位置: `UserManagementService.cs` Line 83-100

### 2. **可能的問題原因**
1. ? UserId 遷移過程可能影響了帳號資料
2. ? 密碼 Hash 演算法正確 (SHA256 + Salt)
3. ?? 登入時需要使用 `UID-000001` 或 `SuperAdmin` (兩者都可以)

## 解決方案

### 方案 A: 使用預設密碼登入

**登入資訊**:
- 使用者名稱: `UID-000001` 或 `SuperAdmin`
- 密碼: `superadmin`

### 方案 B: 重置資料庫（強制重建預設帳號）

1. **刪除現有資料庫**:
   ```
   刪除檔案: ModelB.Demo\bin\Debug\net8.0-windows\StackDoseData.db
   ```

2. **重新啟動程式**:
   - 程式會自動建立新的資料庫
   - 自動建立預設 SuperAdmin 帳號
   - 密碼為 `superadmin`

### 方案 C: 使用程式碼重置密碼

建立一個密碼重置工具程式：

```csharp
using Stackdose.UI.Core.Services;
using Stackdose.UI.Core.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. 建立 UserManagementService
        var service = new UserManagementService();
        
        // 2. 取得 SuperAdmin 使用者
        var users = await service.GetAllUsersAsync();
        var superAdmin = users.FirstOrDefault(u => u.UserId == "UID-000001");
        
        if (superAdmin == null)
        {
            Console.WriteLine("找不到 SuperAdmin 帳號");
            return;
        }
        
        // 3. 重置密碼為 "admin123"
        var result = await service.ResetPasswordAsync(
            targetUserId: superAdmin.Id,
            operatorUserId: superAdmin.Id, // 自己重置自己
            newPassword: "admin123"
        );
        
        Console.WriteLine($"密碼重置結果: {result.Success} - {result.Message}");
        
        if (result.Success)
        {
            Console.WriteLine("? 密碼已重置為: admin123");
            Console.WriteLine($"   帳號: {superAdmin.UserId}");
            Console.WriteLine($"   名稱: {superAdmin.DisplayName}");
        }
    }
}
```

## 驗證方式

### 1. **檢查資料庫中的使用者**

使用 SQLite 工具查詢：

```sql
SELECT UserId, DisplayName, IsActive, AccessLevel, CreatedAt 
FROM Users 
ORDER BY AccessLevel DESC;
```

預期結果：
```
UserId       DisplayName    IsActive  AccessLevel  CreatedAt
UID-000001   SuperAdmin     1         5            2025-...
```

### 2. **測試登入**

在登入畫面嘗試：
- 使用者: `UID-000001` 或 `SuperAdmin`
- 密碼: `superadmin`

### 3. **查看 Debug 輸出**

在 Visual Studio 的「輸出」視窗中應該會看到：

```
[UserManagementService] Checking default accounts...
[UserManagementService] SuperAdmin (UID-000001) exists: True/False
[UserManagementService] Current users in database:
  - UID-000001 (SuperAdmin) - Level 5, Active: 1
```

## 常見問題

### Q1: 我忘記密碼了，怎麼辦？
**A**: 使用方案 B 刪除資料庫重建，或使用方案 C 建立密碼重置程式。

### Q2: 登入時顯示「使用者不存在或已停用」
**A**: 
1. 檢查 UserId 是否輸入正確（`UID-000001` 或 `SuperAdmin`）
2. 檢查資料庫中的 `IsActive` 欄位是否為 1
3. 確認資料庫檔案位置正確

### Q3: 登入時顯示「密碼錯誤」
**A**:
1. 確認密碼是 `superadmin`（全小寫）
2. 使用方案 B 重置資料庫
3. 使用方案 C 建立密碼重置程式

### Q4: 我想修改預設密碼
**A**: 修改 `UserManagementService.cs` 中的 Line 88:
```csharp
// 將 "superadmin" 改為您想要的密碼
var (superHash, superSalt) = HashPassword("your_new_password");
```

## 技術細節

### 密碼驗證流程

1. **登入時**:
   ```csharp
   public async Task<(bool Success, string Message, UserAccount? User)> AuthenticateAsync(string userId, string password)
   {
       // 1. 查詢使用者（支援 UserId 或 DisplayName）
       var user = await conn.QueryFirstOrDefaultAsync<UserAccount>(
           "SELECT * FROM Users WHERE (UserId = @UserId OR DisplayName = @UserId) AND IsActive = 1",
           new { UserId = userId });
       
       // 2. 驗證密碼
       if (!VerifyPassword(password, user.PasswordHash, user.Salt))
       {
           return (false, "密碼錯誤", null);
       }
       
       return (true, "驗證成功", user);
   }
   ```

2. **密碼 Hash 演算法**:
   ```csharp
   private (string Hash, string Salt) HashPassword(string password)
   {
       // 生成 32-byte Salt
       byte[] saltBytes = new byte[32];
       using (var rng = RandomNumberGenerator.Create())
       {
           rng.GetBytes(saltBytes);
       }
       string salt = Convert.ToBase64String(saltBytes);
       
       // 計算 SHA256(password + salt)
       using (var sha256 = SHA256.Create())
       {
           var passwordWithSalt = Encoding.UTF8.GetBytes(password + salt);
           var hashBytes = sha256.ComputeHash(passwordWithSalt);
           string hash = Convert.ToBase64String(hashBytes);
           return (hash, salt);
       }
   }
   ```

3. **UserId 遷移機制**:
   - 舊格式: `admin`, `user1`, `operator1`
   - 新格式: `UID-000001`, `UID-000002`, `UID-000003`
   - 遷移時會保留原密碼 Hash 和 Salt

## 建議操作步驟

1. ? **第一步**: 嘗試使用預設密碼登入
   - 使用者: `SuperAdmin`
   - 密碼: `superadmin`

2. ? **如果失敗**: 刪除資料庫重建
   - 刪除 `StackDoseData.db`
   - 重新啟動程式

3. ? **驗證成功後**: 立即修改密碼
   - 進入使用者管理頁面
   - 重置 SuperAdmin 密碼為強密碼

## 聯絡支援

如果以上方案都無法解決問題，請提供以下資訊：
1. Visual Studio 輸出視窗的完整 Debug 訊息
2. 資料庫檔案位置
3. 登入時輸入的使用者名稱和密碼（不要貼出實際密碼）
