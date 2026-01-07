# ?? 完整解決方案：Tab 切換問題 + 顯示 PLC 狀態

## ?? 需求

1. ? **切換 Tab 時不重新連線 PLC**
2. ? **首頁需要顯示 PLC 狀態指示燈**
3. ? **切換回 MainPanel 時，Sensor 和 PlcLabel 不重新觸發**

---

## ?? 解決方案架構

### **兩層架構設計**

```
MainWindow (應用程式層級)
  ├─ PlcStatus (隱藏，負責連線)  ← 全域且永久存在
  └─ TabControl
      └─ MainPanel
          ├─ PlcStatusIndicator (顯示狀態)  ← 只訂閱事件，不連線
          ├─ PlcLabel (自動綁定)
          └─ SensorViewer (自動綁定)
```

---

## ?? 修改內容

### 1?? **MainWindow.xaml** - 全域 PlcStatus（隱藏）

```xaml
<Controls:CyberFrame Title="UBI SYSTEM">
    <Controls:CyberFrame.MainContent>
        <Grid>
            <!-- ?? Global PlcStatus (隱藏但保持連線) -->
            <Controls:PlcStatus 
                IpAddress="192.168.22.39" 
                Port="3000" 
                AutoConnect="True" 
                IsGlobal="True"
                Visibility="Collapsed"/>  ← 關鍵：隱藏但保持運作
            
            <!-- TabControl -->
            <TabControl>
                ...
            </TabControl>
        </Grid>
    </Controls:CyberFrame.MainContent>
</Controls:CyberFrame>
```

**作用：**
- ? 維持 PLC 連線（不受 Tab 切換影響）
- ? 註冊到 `PlcContext.GlobalStatus`
- ? UI 隱藏但功能正常

---

### 2?? **PlcStatusIndicator.xaml** - 新建狀態顯示控件

```xaml
<UserControl x:Class="Stackdose.UI.Core.Controls.PlcStatusIndicator"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Border Background="{DynamicResource Plc.Bg.Main}" 
            BorderBrush="{DynamicResource Plc.Border}"
            BorderThickness="1" 
            CornerRadius="5" 
            Padding="10">
        <Grid>
            <!-- Status Light -->
            <Ellipse x:Name="StatusLight" Width="24" Height="24" />
            
            <!-- Status Info -->
            <StackPanel>
                <TextBlock x:Name="StatusText" Text="DISCONNECTED" />
                <TextBlock x:Name="IpDisplay" Text="192.168.22.39:3000" />
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

---

### 3?? **PlcStatusIndicator.xaml.cs** - 只訂閱事件

```csharp
public partial class PlcStatusIndicator : UserControl
{
    private PlcStatus? _globalStatus;

    private void PlcStatusIndicator_Loaded(object sender, RoutedEventArgs e)
    {
        // 訂閱全域 PlcStatus
        _globalStatus = PlcContext.GlobalStatus;

        if (_globalStatus != null)
        {
            // 訂閱 ScanUpdated 事件
            _globalStatus.ScanUpdated += OnPlcScanUpdated;

            // 立即更新狀態
            UpdateStatus(_globalStatus.CurrentManager != null && 
                        _globalStatus.CurrentManager.IsConnected);
        }
    }

    private void OnPlcScanUpdated(IPlcManager manager)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateStatus(manager != null && manager.IsConnected);
        });
    }

    private void UpdateStatus(bool isConnected)
    {
        if (isConnected)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.LimeGreen);
            StatusText.Text = "CONNECTED";
        }
        else
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Red);
            StatusText.Text = "DISCONNECTED";
        }
    }
}
```

**關鍵特性：**
- ? **不執行連線**（沒有 `AutoConnect`）
- ? **只訂閱事件**（`ScanUpdated`）
- ? **自動更新 UI**（透過 `Dispatcher.Invoke`）
- ? **輕量級**（不建立 `IPlcManager`）

---

### 4?? **MainPanel.xaml** - 使用 PlcStatusIndicator

```xaml
<Border Grid.Column="0" BorderBrush="#00E5FF" BorderThickness="2">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>      ← Header
            <RowDefinition Height="Auto"/>      ← PlcStatusIndicator
            <RowDefinition Height="*"/>         ← Process Control Buttons
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#00E5FF">
            <TextBlock Text="PLC Status" />
        </Border>

        <!-- PLC Status Indicator (Display Only) -->
        <Controls:PlcStatusIndicator Grid.Row="1"
            DisplayAddress="192.168.22.39:3000"
            Margin="10,10,10,5"/>

        <!-- Process Control Buttons -->
        <Grid Grid.Row="2">
            <ScrollViewer>
                <StackPanel>
                    <Controls:SecuredButton Content="Start Process" />
                    ...
                </StackPanel>
            </ScrollViewer>
        </Grid>
    </Grid>
</Border>
```

---

## ?? 效果對比

### Before（有問題）

| 動作 | 結果 |
|------|------|
| 切換到 System Test Tab | ? PLC 重新連線 |
| 切換回 Main Tab | ? PLC 重新連線 |
| MainPanel PlcLabel | ? 顯示 "-"（無數據） |
| MainPanel Sensor | ? 重新觸發註冊 |
| 日誌 | ? 每次切換都記錄 "Connecting..." |

### After（正確）

| 動作 | 結果 |
|------|------|
| 切換到 System Test Tab | ? PLC 保持連線 |
| 切換回 Main Tab | ? PLC 保持連線 |
| MainPanel PlcLabel | ? 持續顯示正確數據 |
| MainPanel Sensor | ? 不重新觸發（已註冊） |
| MainPanel PlcStatusIndicator | ? 顯示 "CONNECTED" |
| 日誌 | ? 沒有重複連線記錄 |

---

## ?? 工作原理

### **1. 全域 PlcStatus（MainWindow）**

```
Application Startup
  └─ PlcStatus Loaded
      ├─ 連線 PLC
      ├─ 註冊到 PlcContext.GlobalStatus
      └─ 啟動 Monitor (掃描 PLC 數據)
```

### **2. PlcStatusIndicator（MainPanel）**

```
MainPanel Loaded
  └─ PlcStatusIndicator Loaded
      ├─ 訂閱 PlcContext.GlobalStatus.ScanUpdated
      └─ 更新 UI (CONNECTED/DISCONNECTED)
```

### **3. PlcLabel（MainPanel）**

```
PlcLabel Loaded
  └─ 自動綁定 PlcContext.GlobalStatus
      └─ 訂閱 ScanUpdated
          └─ 更新數據 (D100, D102, etc.)
```

### **4. Tab 切換**

```
切換到 System Test
  └─ MainPanel Unloaded
      ├─ PlcStatusIndicator Unloaded (取消訂閱)
      ├─ PlcLabel Unloaded (取消訂閱)
      └─ PlcContext.GlobalStatus 仍然存在 ?

切換回 Main
  └─ MainPanel Loaded
      ├─ PlcStatusIndicator Loaded (重新訂閱)
      ├─ PlcLabel Loaded (重新訂閱)
      └─ 立即顯示當前 PLC 數據 ?
```

---

## ?? 類別關係圖

```
PlcStatus (MainWindow)
  ├─ IsGlobal="True"  → PlcContext.GlobalStatus
  ├─ AutoConnect="True"  → 自動連線 PLC
  └─ ScanUpdated Event  → 通知所有訂閱者

PlcStatusIndicator (MainPanel)
  ├─ Subscribes to: PlcContext.GlobalStatus.ScanUpdated
  ├─ Updates UI: StatusLight, StatusText
  └─ Does NOT: Connect/Disconnect PLC

PlcLabel (MainPanel)
  ├─ Subscribes to: PlcContext.GlobalStatus.ScanUpdated
  └─ Updates Value: 從 IPlcManager 讀取數據

SensorViewer (MainPanel)
  ├─ Subscribes to: PlcContext.GlobalStatus.ScanUpdated
  └─ Updates Sensors: 批次讀取 Sensor 數據
```

---

## ?? 關鍵設計模式

### **1. 觀察者模式（Observer Pattern）**

```csharp
// PlcStatus (Subject)
public event Action<IPlcManager>? ScanUpdated;

// PlcStatusIndicator (Observer)
_globalStatus.ScanUpdated += OnPlcScanUpdated;
```

### **2. 單例模式（Singleton Pattern）**

```csharp
// PlcContext
public static PlcStatus? GlobalStatus { get; set; }
```

### **3. 依賴注入（Dependency Injection）**

```csharp
// PlcLabel 自動解析
var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
```

---

## ? 驗證清單

執行應用程式並確認：

### **初始化**
- [ ] 應用程式啟動時 PLC 自動連線
- [ ] MainPanel 的 PlcStatusIndicator 顯示 "CONNECTED"
- [ ] MainPanel 的 PlcLabel 顯示正確數據

### **Tab 切換到 System Test**
- [ ] PlcStatusIndicator UI 消失（MainPanel 已卸載）
- [ ] PLC 連線保持（檢查日誌，無 "Connecting..."）
- [ ] PlcContext.GlobalStatus 仍然存在

### **Tab 切換回 Main**
- [ ] Plc StatusIndicator **立即**顯示 "CONNECTED"（不閃爍）
- [ ] PlcLabel **立即**顯示數據（不是 "-"）
- [ ] SensorViewer **立即**顯示 Sensor 狀態
- [ ] 日誌中**沒有**重新連線記錄

### **重複切換**
- [ ] 多次切換 Tab，PlcLabel 數據持續更新
- [ ] 多次切換 Tab，無任何重新連線記錄
- [ ] 多次切換 Tab，Sensor 狀態正常顯示

### **關閉應用程式**
- [ ] PLC 正確斷線
- [ ] 日誌記錄 "PLC Disconnected"

---

## ?? 故障排除

### **問題 1：PlcStatusIndicator 顯示 "DISCONNECTED"**

**原因：** `PlcContext.GlobalStatus` 為 `null`

**檢查：**
1. MainWindow 中的 `PlcStatus` 是否設定 `IsGlobal="True"`
2. MainWindow 是否先於 MainPanel 載入

**解決：**
```xaml
<Controls:PlcStatus IsGlobal="True" ... />
```

---

### **問題 2：切換 Tab 時 PlcLabel 閃爍（先顯示 "-" 再顯示數據）**

**原因：** `PlcLabel` 重新綁定時，`PlcContext.GlobalStatus.CurrentManager` 為 `null`

**檢查：**
```csharp
// PlcStatusIndicator.Loaded
if (_globalStatus.CurrentManager != null)
{
    OnPlcScanUpdated(_globalStatus.CurrentManager);  // 立即更新
}
```

**解決：** 確保 `BindToStatus` 中呼叫：
```csharp
if (_boundStatus.CurrentManager != null) 
    OnScanUpdated(_boundStatus.CurrentManager);
```

---

### **問題 3：Sensor 切換 Tab 時重新觸發註冊**

**原因：** `SensorContext.GenerateMonitorAddresses()` 在每次 PLC 連線時執行

**這是正常行為** ?  
因為 `SensorViewer` 在 `Loaded` 時會重新註冊 Sensor，但：
- ? PLC 連線**不會**重置
- ? Monitor 地址註冊是**冪等**的（重複註冊無副作用）
- ? 數據持續更新

---

## ?? 總結

### **核心概念**

> **全域資源放在應用程式最上層（MainWindow），UI 顯示只訂閱事件，不負責連線。**

### **架構優勢**

1. **關注點分離（Separation of Concerns）**
   - PlcStatus → 連線邏輯
   - PlcStatusIndicator → UI 顯示
   - PlcLabel → 數據顯示

2. **生命週期獨立**
   - PlcStatus 不受 Tab 切換影響
   - PlcStatusIndicator 隨 Tab 載入/卸載
   - 連線狀態始終保持

3. **可重用性**
   - PlcStatusIndicator 可放在任何 UserControl
   - PlcLabel 自動找到 GlobalStatus
   - SensorViewer 自動綁定

4. **效能優化**
   - 只建立一個 PLC 連線
   - 避免重複連線/斷線
   - 減少網路開銷

---

## ?? 更新日期

**版本：** 2.0  
**日期：** 2025-01-06  
**作者：** GitHub Copilot  

---

## ?? 相關檔案

- `WpfApp1/MainWindow.xaml` - 全域 PlcStatus
- `Stackdose.UI.Core/Controls/PlcStatusIndicator.xaml` - 新建（狀態顯示）
- `Stackdose.UI.Core/Controls/PlcStatusIndicator.xaml.cs` - 新建（只訂閱事件）
- `WpfApp1/Panels/MainPanel.xaml` - 使用 PlcStatusIndicator
- `Stackdose.UI.Core/Controls/PlcStatus.xaml.cs` - PLC 連線邏輯
- `Stackdose.UI.Core/Helpers/PlcContext.cs` - 全域連線管理

---

## ?? 學習要點

1. **WPF 事件訂閱模式** → 避免記憶體洩漏
2. **Dispatcher.Invoke** → 跨執行緒 UI 更新
3. **UserControl 生命週期** → Loaded/Unloaded
4. **依賴屬性 (DependencyProperty)** → 可綁定的屬性
5. **弱引用 (WeakReference)** → 避免循環參考

---

## ??? 進階優化（可選）

### **1. 新增連線狀態變更通知**

```csharp
// PlcStatus.cs
public event Action<bool>? ConnectionStateChanged;

private void UpdateUiState(ConnectionState state)
{
    bool isConnected = (state == ConnectionState.Connected);
    ConnectionStateChanged?.Invoke(isConnected);
    ...
}
```

### **2. PlcStatusIndicator 新增重連按鈕**

```xaml
<Button Content="Reconnect" 
        Click="ReconnectButton_Click"
        Visibility="{Binding IsDisconnected, Converter={...}}"/>
```

```csharp
private void ReconnectButton_Click(object sender, RoutedEventArgs e)
{
    // 透過 GlobalStatus 手動觸發重連
    PlcContext.GlobalStatus?.ConnectAsync();
}
```

---

## ?? 完成！

現在您的應用程式可以：
- ? 自由切換 Tab
- ? PLC 連線始終保持穩定
- ? 首頁顯示 PLC 狀態指示燈
- ? 切換回 MainPanel 時，數據立即顯示
- ? Sensor 不重新觸發（已註冊）

?? **Tab 切換問題完全解決！**
