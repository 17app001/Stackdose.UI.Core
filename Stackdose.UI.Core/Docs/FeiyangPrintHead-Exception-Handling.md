# Exception 處理策略：Connect() 方法

## ?? 問題描述

當 `FeiyangPrintHead.Connect()` 內部發生錯誤時，外部調用者如何取得詳細錯誤資訊？

---

## ?? 解決方案比較

### **方案 1：內部捕捉 + LastErrorMessage（目前採用）**

#### **FeiyangPrintHead.cs**
```csharp
public string? LastErrorMessage { get; private set; }

public async Task<bool> Connect()
{
    try
    {
        // 連線邏輯
        LastErrorMessage = null;
        return true;
    }
    catch (Exception ex)
    {
        LastErrorMessage = ex.Message;
        _connected = false;
        return false;
    }
}
```

#### **外部調用（PrintHeadStatus.xaml.cs）**
```csharp
bool connected = await _printHead.Connect();

if (connected)
{
    // 成功
}
else
{
    // 取得錯誤訊息
    string errorMsg = _printHead.LastErrorMessage ?? "Unknown error";
    UpdateStatus(false, $"Connection failed: {errorMsg}");
}
```

#### **優點：**
? API 簡單，回傳 `bool` 容易理解  
? 不會中斷程式流程  
? 適合「嘗試連線」的場景

#### **缺點：**
? 需要額外檢查 `LastErrorMessage`  
? 無法取得原始 `Exception` 物件  
? 多執行緒環境可能有競爭條件

---

### **方案 2：重新拋出 Exception**

#### **FeiyangPrintHead.cs**
```csharp
public async Task<bool> Connect()
{
    try
    {
        // 連線邏輯
        return true;
    }
    catch (Exception ex)
    {
        LastErrorMessage = ex.Message;
        _connected = false;
        
        // 重新拋出
        throw;
    }
}
```

#### **外部調用**
```csharp
try
{
    bool connected = await _printHead.Connect();
    // 成功處理
}
catch (InvalidOperationException ex)
{
    // 設定檔錯誤
    UpdateStatus(false, $"Config error: {ex.Message}");
}
catch (Exception ex)
{
    // 其他錯誤
    UpdateStatus(false, $"Connection error: {ex.Message}");
}
```

#### **優點：**
? 錯誤資訊完整（包含 StackTrace）  
? 可以區分不同類型的錯誤  
? 符合 .NET 異常處理慣例

#### **缺點：**
? 強制外部使用 `try-catch`  
? 可能中斷程式流程  
? 回傳值變成「是否執行成功」而非「是否連線」

---

### **方案 3：Result Pattern**

#### **定義結果類型**
```csharp
public class ConnectionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static ConnectionResult Ok() 
        => new() { Success = true };

    public static ConnectionResult Fail(string message, Exception? ex = null)
        => new() { Success = false, ErrorMessage = message, Exception = ex };
}
```

#### **FeiyangPrintHead.cs**
```csharp
public async Task<ConnectionResult> Connect()
{
    try
    {
        // 連線邏輯
        return ConnectionResult.Ok();
    }
    catch (Exception ex)
    {
        _connected = false;
        return ConnectionResult.Fail(ex.Message, ex);
    }
}
```

#### **外部調用**
```csharp
var result = await _printHead.Connect();

if (result.Success)
{
    UpdateStatus(true, "CONNECTED");
}
else
{
    UpdateStatus(false, $"Failed: {result.ErrorMessage}");
    
    // 可取得原始 Exception
    if (result.Exception != null)
    {
        LogException(result.Exception);
    }
}
```

#### **優點：**
? 明確的成功/失敗語義  
? 包含完整錯誤資訊  
? 不依賴 Exception 控制流程  
? 支援多執行緒

#### **缺點：**
? 需要定義額外類型  
? 增加 API 複雜度  
? 團隊需要理解這個模式

---

## ?? 目前實作（方案 1）

### **原因：**

1. **API 簡單**
   - `bool connected = await _printHead.Connect();` 容易理解
   - 適合 WPF 控制項的使用場景

2. **錯誤處理彈性**
   - UI 層可以自行決定如何顯示錯誤
   - 不會因為 Exception 中斷整個連線流程

3. **一致性**
   - 符合現有專案的錯誤處理風格
   - 其他類似方法（如 PLC 連線）也是這樣設計

### **使用方式：**

```csharp
// 建立實例
_printHead = new FeiyangPrintHead(ConfigFilePath);

// 嘗試連線
bool connected = await _printHead.Connect();

if (connected)
{
    // ? 連線成功
    UpdateStatus(true, "CONNECTED");
    StartTemperatureMonitoring();
}
else
{
    // ? 連線失敗，取得錯誤訊息
    string errorMsg = _printHead.LastErrorMessage ?? "Unknown error";
    UpdateStatus(false, $"Connection failed: {errorMsg}");
    ConnectionLost?.Invoke(errorMsg);
}
```

---

## ?? 關鍵重點

### **內部（FeiyangPrintHead.cs）：**
- ? 捕捉所有 Exception
- ? 設定 `LastErrorMessage`
- ? 更新內部狀態（`_connected`、`State`）
- ? 記錄日誌（`LogInfo`）
- ? 回傳 `false`

### **外部（PrintHeadStatus.xaml.cs）：**
- ? 檢查 `connected == false`
- ? 讀取 `_printHead.LastErrorMessage`
- ? 更新 UI 狀態
- ? 觸發事件（`ConnectionLost`）
- ? 記錄 Compliance 日誌

---

## ?? 未來改進建議

如果未來需要更豐富的錯誤處理（例如：重試機制、網路逾時偵測），可以考慮：

1. **擴展 Result Pattern**
   ```csharp
   public enum ConnectionErrorType
   {
       ConfigInvalid,
       NetworkTimeout,
       BoardNotResponding,
       FirmwareError
   }

   public class ConnectionResult
   {
       public bool Success { get; init; }
       public ConnectionErrorType? ErrorType { get; init; }
       public string? ErrorMessage { get; init; }
   }
   ```

2. **支援重試機制**
   ```csharp
   public async Task<bool> ConnectWithRetry(int maxRetries = 3, int delayMs = 1000)
   {
       for (int i = 0; i < maxRetries; i++)
       {
           bool connected = await Connect();
           if (connected) return true;
           
           await Task.Delay(delayMs);
       }
       return false;
   }
   ```

---

## ? 檢查清單

使用 `Connect()` 方法時：

- [ ] 檢查回傳值 `bool connected`
- [ ] 如果 `connected == false`，讀取 `LastErrorMessage`
- [ ] 更新 UI 狀態（連線失敗）
- [ ] 觸發錯誤事件（`ConnectionLost?.Invoke(errorMsg)`）
- [ ] 記錄日誌（`ComplianceContext.LogSystem`）

---

**最後更新：** 2025/01/18  
**適用版本：** FeiyangPrintHead v1.0
