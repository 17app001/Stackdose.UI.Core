# ?? 登入後程式跳掉 - 快速診斷清單

## ? 最新修改

### **MainWindow.xaml.cs**
- ? 所有方法都包裹在 try-catch 中
- ? 事件訂閱移到 `Loaded` 事件中（避免時序問題）
- ? 每一步都有 Debug 日誌輸出

### **App.xaml.cs**
- ? 所有例外都會被攔截並寫入 `app_startup.log`
- ? 顯示友善的錯誤 MessageBox

---

## ?? 現在請按照以下步驟測試

### **步驟 1：執行應用程式（F5）**

### **步驟 2：登入**
- 使用 Admin / admin123
- 或您的 Windows 使用者名稱 + Windows 密碼

### **步驟 3：觀察結果**

#### **情況 A：程式正常顯示**
? 恭喜！問題已解決！

#### **情況 B：程式自動關閉**
請立即執行以下步驟：

---

## ?? 情況 B - 查看日誌檔案

### **1. 開啟檔案總管**
導航到：
```
D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\
```

### **2. 開啟 `app_startup.log`**
用記事本或任何文字編輯器開啟

### **3. 複製最後 20 行**
貼到對話中給我

---

## ?? 日誌檔案範例

### ? **正常情況（成功）**
```
[09:30:15.123] OnStartup: Called
[09:30:15.234] Initializing UserManagementService...
[09:30:15.345] UserManagementService initialized
[09:30:15.456] Showing login dialog...
[09:30:20.567] Login dialog closed - Success: True
[09:30:20.678] Login successful: Admin (Admin)
[09:30:20.789] Creating MainWindow...
[09:30:20.890] MainWindow created, calling Show()...
[09:30:20.901] MainWindow.Show() completed successfully!
[09:30:21.012] Application startup COMPLETED
```

### ? **異常情況 1 - MainWindow 建構失敗**
```
[09:30:20.789] Creating MainWindow...
[09:30:20.890] FATAL ERROR in OnStartup: Cannot find resource named 'Cyber.Bg.Panel'
[09:30:20.901] Exception Type: System.Windows.ResourceReferenceKeyNotFoundException
```
**→ 原因：資源檔案 (Colors.xaml) 載入失敗**

### ? **異常情況 2 - MainWindow InitializeComponent 失敗**
```
[09:30:20.789] Creating MainWindow...
[09:30:20.890] FATAL ERROR in OnStartup: The name 'CyberFrame' does not exist in the namespace
[09:30:20.901] Exception Type: System.Windows.Markup.XamlParseException
```
**→ 原因：XAML 解析失敗，可能是控制項命名空間問題**

### ? **異常情況 3 - MainViewModel 建立失敗**
```
[09:30:20.789] Creating MainWindow...
[09:30:20.890] [MainWindow] Constructor: Start
[09:30:20.901] [MainWindow] Constructor: InitializeComponent completed
[09:30:20.912] FATAL ERROR in OnStartup: Object reference not set to an instance of an object
```
**→ 原因：MainViewModel 建構函數有問題**

---

## ?? 快速修復方法

### **如果錯誤是：Cannot find resource**
**問題：** Colors.xaml 資源檔案未正確載入

**檢查：**
```xaml
<!-- 在 App.xaml 中確認 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Stackdose.UI.Core;component/Themes/Colors.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

**修復方式：**
1. 確認 `Stackdose.UI.Core\Themes\Colors.xaml` 檔案存在
2. 確認檔案屬性設為「Page」
3. 重新建置專案

---

### **如果錯誤是：XamlParseException**
**問題：** XAML 語法錯誤或控制項命名空間錯誤

**檢查：**
```xaml
<!-- 在 MainWindow.xaml 中確認 -->
xmlns:Controls="http://schemas.stackdose.com/wpf"
```

**修復方式：**
1. 確認 Stackdose.UI.Core 專案已正確參考
2. 確認 CyberFrame 控制項存在
3. 清理並重新建置方案

---

### **如果錯誤是：Object reference not set**
**問題：** MainViewModel 或其他物件初始化失敗

**檢查 MainViewModel.cs：**
```csharp
public MainViewModel()
{
    // 確保所有屬性都有初始化
}
```

---

## ?? 如果日誌檔案不存在

**可能原因：**
1. 程式在建立日誌檔案前就崩潰了
2. 檔案寫入權限問題

**解決方式：**
1. 以「系統管理員身分執行」Visual Studio
2. 重新執行應用程式

---

## ?? 臨時測試方法

如果想先跳過登入測試 MainWindow 是否能正常顯示：

```csharp
// 在 App.xaml.cs 的 OnStartup 中
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    try
    {
        WriteLog("TESTING: Skipping login, directly showing MainWindow");
        
        // ?? 直接登入
        SecurityContext.QuickLogin(AccessLevel.Admin);
        
        // ?? 直接顯示 MainWindow
        var mainWindow = new MainWindow();
        mainWindow.Show();
        
        WriteLog("TESTING: MainWindow shown successfully");
    }
    catch (Exception ex)
    {
        WriteLog($"TESTING ERROR: {ex.Message}");
        WriteLog($"Stack Trace: {ex.StackTrace}");
        MessageBox.Show($"測試失敗: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        this.Shutdown();
    }
}
```

---

## ?? 需要提供的資訊

如果問題仍然存在，請提供：

1. **`app_startup.log` 的最後 20 行**
2. **是否有看到任何 MessageBox 錯誤訊息**（截圖）
3. **Visual Studio 輸出視窗的內容**（如果有開啟）
4. **使用的帳號**（Admin 或 Windows 使用者）

---

## ? 建置成功！

請執行應用程式並回報結果。

**預設帳號：**
- User ID: `Admin`
- Password: `admin123`

**日誌檔案位置：**
```
D:\工作區\Project\Stackdose.UI.Core\WpfApp1\bin\Debug\net8.0-windows\app_startup.log
```

祝測試順利！??
