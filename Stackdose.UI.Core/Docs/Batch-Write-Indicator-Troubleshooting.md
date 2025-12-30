# 批次寫入狀態燈號疑難排解指南

## 問題：看不到燈號變綠色和初始化訊息

### ?? 可能原因

#### 1. ComplianceContext 未被初始化

**問題：** `SqliteLogger.Initialize()` 是在 `ComplianceContext` 的靜態建構函數中呼叫的，只有在第一次使用 `ComplianceContext` 時才會執行。

**檢查方法：**

在 `MainWindow.xaml.cs` 的 `MainWindow()` 建構函數中加入：

```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ?? 強制初始化 ComplianceContext（觸發靜態建構函數）
    ComplianceContext.LogSystem("Application Started", LogLevel.Info);
    
    // 其他初始化...
}
```

---

#### 2. CyberFrame 建構函數執行順序問題

**問題：** `CyberFrame` 的 `InitializeBatchWriteIndicator()` 可能在 `SqliteLogger.Initialize()` 之前執行。

**解決方案：** 改用 `Loaded` 事件訂閱

**修改 CyberFrame.xaml.cs：**

```csharp
public CyberFrame()
{
    InitializeComponent();

    InitializeClock();
    InitializeSecurityEvents();
    // InitializeBatchWriteIndicator(); // ? 移除

    UpdateUserInfo();

    // ? 改用 Loaded 事件
    this.Loaded += CyberFrame_Loaded;
    this.Unloaded += CyberFrame_Unloaded;
}

// ? 新增 Loaded 事件處理
private void CyberFrame_Loaded(object sender, RoutedEventArgs e)
{
    // 確保 ComplianceContext 已初始化
    ComplianceContext.LogSystem("[CyberFrame] Initializing...", Models.LogLevel.Info, showInUi: false);
    
    // 訂閱批次寫入事件
    InitializeBatchWriteIndicator();
}
```

---

#### 3. 沒有實際的批次寫入發生

**問題：** 燈號只有在**真正發生批次寫入**時才會變綠色。

**測試方法：**

1. 啟動程式
2. 點擊 **"Test Batch Write (500 logs)"** 按鈕
3. 觀察燈號是否短暫變綠

或

1. 連接 PLC
2. 讓 PlcLabel 開始記錄數據（`EnableDataLog="True"`）
3. 等待累積 100 筆或 5 秒
4. 觀察燈號是否變綠

---

#### 4. Debug 模式未啟用

**問題：** 所有 Debug 輸出都被 `#if DEBUG` 包住了，Release 模式下看不到。

**檢查方法：**

確認你是在 **Debug 模式** 下運行：

```
Visual Studio → 上方工具列 → 確認是 "Debug" 不是 "Release"
```

---

## ?? 完整驗證步驟

### 步驟 1：確認初始化順序

在 `MainWindow.xaml.cs` 加入強制初始化：

```csharp
public MainWindow()
{
    InitializeComponent();

    // ?? 1. 強制初始化 ComplianceContext（觸發 SqliteLogger.Initialize）
    ComplianceContext.LogSystem("========== Application Starting ==========", LogLevel.Info);
    
    // 2. 顯示批次寫入設定
    var (dataLogs, auditLogs, flushes, pending, pendingAudit) = ComplianceContext.GetBatchStatistics();
    ComplianceContext.LogSystem($"Batch Write Status: Total={dataLogs+auditLogs}, Pending={pending+pendingAudit}", LogLevel.Info);
    
    // 3. 原有的初始化...
    SecurityContext.QuickLogin(AccessLevel.Engineer);
    
    _viewModel = new MainViewModel();
    DataContext = _viewModel;
    
    // ...
}
```

### 步驟 2：修改 CyberFrame 使用 Loaded 事件

**CyberFrame.xaml.cs：**

```csharp
public CyberFrame()
{
    InitializeComponent();

    InitializeClock();
    InitializeSecurityEvents();
    UpdateUserInfo();

    // ? 改用 Loaded 事件
    this.Loaded += CyberFrame_Loaded;
    this.Unloaded += CyberFrame_Unloaded;
}

private void CyberFrame_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        // 確保 ComplianceContext 已初始化
        ComplianceContext.LogSystem("[CyberFrame] Loaded, initializing batch write indicator...", 
            Models.LogLevel.Info, showInUi: false);
        
        // 訂閱批次寫入事件
        SqliteLogger.BatchFlushStarted += OnBatchFlushStarted;
        SqliteLogger.BatchFlushCompleted += OnBatchFlushCompleted;
        
        #if DEBUG
        System.Diagnostics.Debug.WriteLine("[CyberFrame] 批次寫入狀態燈號已初始化");
        #endif
    }
    catch (Exception ex)
    {
        #if DEBUG
        System.Diagnostics.Debug.WriteLine($"[CyberFrame] CyberFrame_Loaded Error: {ex.Message}");
        #endif
    }
}
```

### 步驟 3：加入立即測試

在 `MainWindow.xaml.cs` 加入自動測試：

```csharp
private void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    // ? 自動測試批次寫入（產生一些數據）
    Task.Run(() =>
    {
        Thread.Sleep(2000); // 等待 2 秒讓 UI 完全載入
        
        for (int i = 0; i < 10; i++)
        {
            ComplianceContext.LogDataHistory($"AutoTest_{i}", $"D{i}", i.ToString());
            Thread.Sleep(100);
        }
        
        // 手動觸發刷新
        ComplianceContext.FlushLogs();
    });
}
```

**在 MainWindow.xaml 加入：**

```xml
<Window ... Loaded="MainWindow_Loaded">
```

---

## ?? 預期的 Debug 輸出

如果一切正常，你應該看到：

```
[ComplianceContext] 合規引擎已啟動（批次寫入模式）
[SqliteLogger] 批次寫入模式已啟用
[SqliteLogger] 批次大小: 100, 刷新間隔: 5000ms
[CyberFrame] 批次寫入狀態燈號已初始化
[CyberFrame] 批次寫入開始: 10+0
[SqliteLogger] DataLogs 批次寫入: 10 筆 (累計: 10)
[CyberFrame] 批次寫入完成: 10+0
```

---

## ?? 快速驗證清單

- [ ] Debug 模式下運行（不是 Release）
- [ ] MainWindow 建構函數中強制初始化 ComplianceContext
- [ ] CyberFrame 改用 Loaded 事件訂閱
- [ ] 點擊 "Test Batch Write" 按鈕
- [ ] 查看 Debug 視窗輸出
- [ ] 觀察燈號是否變綠（短暫）

---

## ?? 如果還是沒有反應

### 檢查事件訂閱

在 `OnBatchFlushStarted` 和 `OnBatchFlushCompleted` 的第一行加入強制輸出：

```csharp
private void OnBatchFlushStarted(int dataCount, int auditCount)
{
    // ? 強制輸出（不受 #if DEBUG 影響）
    Console.WriteLine($"[OnBatchFlushStarted] dataCount={dataCount}, auditCount={auditCount}");
    
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[OnBatchFlushStarted] Error: {ex.Message}");
    }
}
```

### 檢查 BatchWriteIndicator 是否存在

在 `SetBatchWriteIndicatorColor` 加入診斷：

```csharp
private void SetBatchWriteIndicatorColor(Color color)
{
    try
    {
        var batchWriteIndicator = this.FindName("BatchWriteIndicator") as Border;
        
        Console.WriteLine($"[SetBatchWriteIndicatorColor] Indicator found: {batchWriteIndicator != null}");
        Console.WriteLine($"[SetBatchWriteIndicatorColor] Color: {color}");
        
        if (batchWriteIndicator == null)
        {
            Console.WriteLine("[SetBatchWriteIndicatorColor] ERROR: BatchWriteIndicator not found!");
            return;
        }
        
        // ...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SetBatchWriteIndicatorColor] Error: {ex.Message}");
    }
}
```

---

**如果按照這些步驟還是無法解決，請提供：**
1. Debug 視窗的完整輸出
2. 你執行的操作步驟
3. 是否有任何錯誤訊息

我會進一步協助你診斷問題！
