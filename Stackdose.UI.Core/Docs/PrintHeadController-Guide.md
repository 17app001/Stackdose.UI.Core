# PrintHeadController 使用指南

## 概述

`PrintHeadController` 是一個統一控制所有已連接噴頭的控制器元件，可以批量執行閃噴、編碼器重置、圖形載入和列印控制等操作。

## 功能特性

### 1. **閃噴控制 (Spit)**
- 頻率 (Frequency): kHz
- 工作時間 (Work Duration): 秒
- 閒置時間 (Idle Duration): 秒
- 液滴數 (Drops): 整數

預設值：0.1kHz, 1s, 1s, 1 drops

### 2. **編碼器重置 (Encoder Reset)**
- 可設定重置值（預設 1000）
- 一鍵重置所有噴頭的編碼器

### 3. **圖形控制 (Image)**
- 支援 BMP、PNG、JPG 格式
- 可瀏覽檔案或直接輸入路徑
- 批量載入圖形到所有噴頭

### 4. **列印控制 (Print)**
- **StartX**: 起始位置 (mm)
- **Direction**: 列印方向
  - 單向 (Uni): 單向列印
  - 雙向 (Bi): 雙向列印
- **Start All**: 啟動所有噴頭
- **Pause All**: 暫停所有噴頭
- **Stop All**: 停止所有噴頭

## XAML 使用範例

```xaml
<Window xmlns:Custom="clr-namespace:Stackdose.UI.Core.Controls;assembly=Stackdose.UI.Core">
    <Grid>
        <!-- PrintHead 控制器 -->
        <Custom:PrintHeadController/>
    </Grid>
</Window>
```

## 完整示例

```xaml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:Custom="clr-namespace:Stackdose.UI.Core.Controls;assembly=Stackdose.UI.Core"
        Title="PrintHead Control Panel" Height="700" Width="1200">
    
    <Grid Margin="10">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="250"/>
            <ColumnDefinition Width="10"/>
            <ColumnDefinition Width="250"/>
            <ColumnDefinition Width="10"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- PrintHead 1 -->
        <Custom:PrintHeadStatus Grid.Column="0"
                                HeadName="PrintHead 1"
                                ConfigFilePath="feiyang_head1.json"
                                AutoConnect="True"/>

        <!-- PrintHead 2 -->
        <Custom:PrintHeadStatus Grid.Column="2"
                                HeadName="PrintHead 2"
                                ConfigFilePath="feiyang_head2.json"
                                AutoConnect="True"/>

        <!-- 統一控制器 -->
        <Custom:PrintHeadController Grid.Column="4"/>
    </Grid>
</Window>
```

## 工作流程

### 1. 閃噴測試流程

```
1. 確保噴頭已連接（檢查右上角 "X Connected"）
2. 調整閃噴參數：
   - Frequency: 0.1 kHz
   - Work Duration: 1 秒
   - Idle Duration: 1 秒
   - Drops: 1
3. 點擊 "Start Spit All" 按鈕
4. 觀察 LiveLogViewer 的執行結果
```

### 2. 編碼器重置流程

```
1. 輸入重置值（預設 1000）
2. 點擊 "Reset All Encoders" 按鈕
3. 所有噴頭的編碼器會重置為指定值
```

### 3. 圖形載入流程

```
1. 點擊 "Browse..." 選擇圖片檔案
   或直接輸入圖片路徑
2. 點擊 "Load Image to All Heads" 按鈕
3. 圖片會載入到所有已連接的噴頭
```

### 4. 列印操作流程

```
1. 設定 StartX 位置（單位：mm）
2. 選擇列印方向（單向/雙向）
3. 點擊 "Start All" 開始列印
4. 需要時可使用 "Pause All" 暫停
5. 點擊 "Stop All" 停止列印
```

## 程式碼範例

### 訂閱 PrintHead 事件

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 訂閱 PrintHead 連線事件
        PrintHeadContext.PrintHeadConnected += OnPrintHeadConnected;
        PrintHeadContext.PrintHeadDisconnected += OnPrintHeadDisconnected;
    }

    private void OnPrintHeadConnected(string name)
    {
        MessageBox.Show($"PrintHead '{name}' 已連接！");
    }

    private void OnPrintHeadDisconnected(string name)
    {
        MessageBox.Show($"PrintHead '{name}' 已斷線！");
    }
}
```

### 取得所有已連接的噴頭

```csharp
// 檢查是否有噴頭連接
if (PrintHeadContext.HasConnectedPrintHead)
{
    int count = PrintHeadContext.ConnectedPrintHeads.Count;
    Console.WriteLine($"已連接 {count} 個噴頭");

    // 列出所有噴頭
    foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
    {
        string name = kvp.Key;
        dynamic printHead = kvp.Value;
        Console.WriteLine($"- {name}");
    }
}
```

### 手動控制特定噴頭

```csharp
// 取得特定噴頭
dynamic? printHead = PrintHeadContext.GetPrintHead("PrintHead 1");

if (printHead != null)
{
    // 執行閃噴
    var spitParams = new SpitParams
    {
        Frequency = 0.1,
        WorkDuration = 1.0,
        IdleDuration = 1.0,
        Drops = 1
    };
    await printHead.Spit(spitParams);

    // 載入圖片
    bool result = printHead.LoadImage("test.bmp");

    // 開始列印
    bool started = printHead.StartPrint();
}
```

## 日誌系統

所有操作都會自動記錄到 `ComplianceContext`：

```
[PrintHeadController] Starting spit on all heads (Freq:0.1kHz, Work:1s, Idle:1s, Drops:1)
[PrintHeadController] PrintHead 1: Spit started successfully
[PrintHeadController] PrintHead 2: Spit started successfully
[PrintHeadController] Spit completed: 2 success, 0 failed
```

## 注意事項

### 1. **連接狀態檢查**
- 操作前會自動檢查是否有已連接的噴頭
- 如果沒有連接，會顯示警告訊息

### 2. **參數驗證**
- 所有輸入框都會進行數值驗證
- 無效數值會顯示錯誤訊息

### 3. **批量操作**
- 所有操作都是批量執行
- 會統計成功/失敗的數量
- 個別失敗不會中斷整體流程

### 4. **執行順序**
```
建議順序：連接 → 閃噴測試 → 編碼器重置 → 載入圖形 → 開始列印
```

## 錯誤處理

### 常見錯誤

1. **"沒有已連接的噴頭"**
   - 確認 PrintHeadStatus 的 AutoConnect="True"
   - 檢查 config 檔案路徑是否正確
   - 確認 IP 和 Port 設定

2. **"參數必須是有效的數字"**
   - 檢查輸入框的數值格式
   - 確認沒有多餘的空格或特殊字元

3. **"圖片檔案不存在"**
   - 使用 Browse 按鈕選擇檔案
   - 確認檔案路徑正確
   - 支援格式：BMP, PNG, JPG

## 進階應用

### 自訂控制邏輯

```csharp
public class CustomPrintHeadController
{
    public async Task SequentialSpit()
    {
        // 依序對每個噴頭執行閃噴
        foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
        {
            dynamic printHead = kvp.Value;
            await printHead.Spit(new SpitParams { Frequency = 0.1 });
            await Task.Delay(2000); // 間隔 2 秒
        }
    }

    public void ApplyStartX(float startX)
    {
        // 對所有噴頭設定相同的 StartX
        foreach (var kvp in PrintHeadContext.ConnectedPrintHeads)
        {
            dynamic printHead = kvp.Value;
            // TODO: 實現 SetStartX 方法
        }
    }
}
```

## 與其他元件整合

### 與 PlcStatus 整合

```xaml
<Grid>
    <!-- PLC 連接 -->
    <Custom:PlcStatus Grid.Row="0" 
                      IpAddress="192.168.1.100" 
                      Port="502" 
                      IsGlobal="True"/>

    <!-- PrintHead 連接 -->
    <Custom:PrintHeadStatus Grid.Row="1" 
                            HeadName="PrintHead 1"
                            AutoConnect="True"/>

    <!-- 統一控制 -->
    <Custom:PrintHeadController Grid.Row="2"/>
</Grid>
```

### 與 LiveLogViewer 整合

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="10"/>
        <ColumnDefinition Width="400"/>
    </Grid.ColumnDefinitions>

    <Custom:PrintHeadController Grid.Column="0"/>
    <Custom:LiveLogViewer Grid.Column="2"/>
</Grid>
```

## 總結

`PrintHeadController` 提供了一個簡潔的介面來統一控制多個噴頭，特別適合：

? 多噴頭系統的批量操作  
? 生產線上的快速測試  
? 參數一致性驗證  
? 自動化流程整合  

所有操作都會自動記錄到日誌系統，確保符合生產追溯要求。
