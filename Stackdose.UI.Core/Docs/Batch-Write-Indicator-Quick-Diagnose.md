## 快速診斷腳本

讓我們建立一個簡單的測試來確認問題：

1. 在 `MainWindow.xaml.cs` 的 `MainWindow()` 建構函數**最後面**加入：

```csharp
// ?? 測試：5 秒後自動觸發批次寫入
Task.Run(async () =>
{
    await Task.Delay(5000); // 等待 5 秒

    Console.WriteLine("========== 開始批次寫入測試 ==========");
    
    // 寫入 150 筆數據（會觸發批次刷新，因為預設 100 筆就刷新）
    for (int i = 0; i < 150; i++)
    {
        ComplianceContext.LogDataHistory($"Test_{i}", $"D{i}", i.ToString());
        await Task.Delay(10);
    }
    
    Console.WriteLine("========== 批次寫入測試完成 ==========");
});
```

2. 啟動程式，等待 5 秒，觀察：
   - Console 視窗是否有輸出
   - 燈號是否變綠
   - Debug 視窗的訊息

---

如果還是沒有反應，請執行以下診斷：

### 診斷步驟 1：確認 Console 視窗

在 `MainWindow()` 的**第一行**加入：

```csharp
public MainWindow()
{
    // ? 顯示 Console 視窗（強制）
    AllocConsole();
    
    InitializeComponent();
    // ...
}

// ? 在 class 最下方加入
[System.Runtime.InteropServices.DllImport("kernel32.dll")]
private static extern bool AllocConsole();
```

### 診斷步驟 2：確認事件訂閱

在 `CyberFrame_Loaded` 加入強制輸出：

```csharp
private void CyberFrame_Loaded(object sender, RoutedEventArgs e)
{
    // ? 強制輸出（Console + Debug）
    Console.WriteLine("========== CyberFrame_Loaded ==========");
    System.Diagnostics.Debug.WriteLine("========== CyberFrame_Loaded ==========");
    
    try
    {
        ComplianceContext.LogSystem("[CyberFrame] Loaded, initializing batch write indicator...", 
            Models.LogLevel.Info, showInUi: false);
        
        Console.WriteLine("訂閱前...");
        InitializeBatchWriteIndicator();
        Console.WriteLine("訂閱後...");
        
        // ? 確認事件訂閱數量
        var startedCount = SqliteLogger.BatchFlushStarted?.GetInvocationList().Length ?? 0;
        var completedCount = SqliteLogger.BatchFlushCompleted?.GetInvocationList().Length ?? 0;
        
        Console.WriteLine($"BatchFlushStarted 訂閱數: {startedCount}");
        Console.WriteLine($"BatchFlushCompleted 訂閱數: {completedCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");
    }
}
```

### 診斷步驟 3：測試事件觸發

在 `OnBatchFlushStarted` 加入強制輸出：

```csharp
private void OnBatchFlushStarted(int dataCount, int auditCount)
{
    // ? 最優先輸出（確認事件有被觸發）
    Console.WriteLine($"========== OnBatchFlushStarted ==========");
    Console.WriteLine($"dataCount={dataCount}, auditCount={auditCount}");
    Console.WriteLine($"Thread: {Thread.CurrentThread.ManagedThreadId}");
    
    try
    {
        Dispatcher.InvokeAsync(() =>
        {
            Console.WriteLine("Dispatcher.InvokeAsync 執行中...");
            SetBatchWriteIndicatorColor(Colors.LimeGreen);
            Console.WriteLine("顏色已設定為綠色");
            
            // ...
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    }
}
```

---

## 我的懷疑

我懷疑問題可能是：

1. **ComplianceContext 的靜態建構函數沒有執行** - 導致 SqliteLogger 沒有初始化
2. **事件訂閱在錯誤的執行緒** - 導致事件無法觸發
3. **BatchWriteIndicator 控制項找不到** - 導致顏色變化失敗

請先執行上面的診斷步驟，然後告訴我：
1. Console 視窗有沒有出現任何輸出？
2. Debug 視窗有沒有任何訊息？
3. 有沒有任何錯誤訊息？

我會根據你的回報進一步協助！
