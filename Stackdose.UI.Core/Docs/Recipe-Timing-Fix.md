# Recipe 載入時序問題修復指南

## 問題描述

當 `RecipeLoader` 設定 `AutoLoadOnStartup="True"` 而 `PlcStatus` 設定 `AutoConnect="False"` 時，會發生時序問題：

```
? 錯誤流程：
1. 應用程式啟動
2. Recipe 自動載入 ?
3. PLC 尚未連線 ?
4. Recipe 監控無法啟動 ?
```

**日誌顯示：**
```
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
System initialized. Main PLC set.  ← PLC 在 Recipe 之後才連線
```

## 解決方案

### 方案 1：延遲 Recipe 載入（簡單）

**MainWindow.xaml**
```xml
<Controls:RecipeLoader 
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="False"  ← 設為 False
    RequiredAccessLevel="Instructor"
    ShowDetails="True"/>
```

**操作流程：**
```
1. 啟動應用程式
2. 手動點擊 PlcStatus 連線 PLC
3. 手動點擊 RecipeLoader 的 Load 按鈕
4. 監控自動啟動 ?
```

### 方案 2：啟用 PLC 自動連線（適合生產環境）

**MainWindow.xaml**
```xml
<Controls:PlcStatus 
    IpAddress="192.168.22.39"
    Port="3000"
    AutoConnect="True"  ← 改為 True
    ScanInterval="120"/>

<Controls:RecipeLoader 
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="True"
    RequiredAccessLevel="Instructor"
    ShowDetails="True"/>
```

**操作流程：**
```
1. 啟動應用程式
2. PLC 自動連線 ?
3. Recipe 自動載入 ?
4. 監控自動啟動 ?
```

### 方案 3：智能自動載入（推薦，已實現）?

在 `MainWindow.xaml.cs` 中訂閱 PLC 連線事件，當 PLC 連線成功時自動載入 Recipe。

**MainWindow.xaml.cs**
```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ? 訂閱 PLC 連線成功事件
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
    
    // 初始化 Recipe 系統（不自動載入）
    _ = InitializeRecipeSystemAsync();
}

private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // 如果 Recipe 還沒載入，自動載入
    if (!RecipeContext.HasActiveRecipe)
    {
        bool success = await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
        
        if (success && RecipeContext.CurrentRecipe != null)
        {
            // 啟動監控
            int registeredCount = RecipeContext.StartMonitoring(plcManager, autoStart: true);
            
            ComplianceContext.LogSystem(
                $"[Recipe] Auto-loaded and monitoring started: {registeredCount} parameters",
                LogLevel.Success,
                showInUi: true
            );
        }
    }
    else
    {
        // Recipe 已經載入，只啟動監控
        if (!RecipeContext.IsMonitoring)
        {
            int registeredCount = RecipeContext.StartMonitoring(plcManager, autoStart: true);
        }
    }
}

private async Task InitializeRecipeSystemAsync()
{
    // 只初始化，不自動載入
    if (!RecipeContext.IsInitialized)
    {
        await RecipeContext.InitializeAsync(autoLoad: false);
    }
    
    RecipeContext.RecipeLoaded += OnRecipeLoaded;
    RecipeContext.RecipeLoadFailed += OnRecipeLoadFailed;
}
```

**MainWindow.xaml**
```xml
<Controls:PlcStatus x:Name="MainPlc"  ← 需要有名稱
    IpAddress="192.168.22.39"
    Port="3000"
    AutoConnect="False"  ← 可以手動或自動
    ScanInterval="120"/>

<Controls:RecipeLoader 
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="False"  ← 設為 False
    RequiredAccessLevel="Instructor"
    ShowDetails="True"/>
```

**優點：**
- ? 無論 PLC 何時連線，都能正確啟動監控
- ? 支援手動和自動連線
- ? 避免時序問題
- ? 自動化流程

**操作流程：**
```
情況 A：PLC 先連線
1. 點擊 PlcStatus 連線 PLC
2. 觸發 ConnectionEstablished 事件
3. 自動載入 Recipe
4. 自動啟動監控 ?

情況 B：Recipe 先載入（手動點擊 Load）
1. 點擊 RecipeLoader 的 Load 按鈕
2. Recipe 載入成功
3. 點擊 PlcStatus 連線 PLC
4. 觸發 ConnectionEstablished 事件
5. 檢測到 Recipe 已載入
6. 只啟動監控 ?

情況 C：Recipe 和 PLC 都已啟動
1. Recipe 已載入
2. PLC 已連線
3. 監控已啟動
4. 無需額外操作 ?
```

## 日誌輸出（正確流程）

```
[PLC] Connecting to PLC (192.168.22.39:3000)...
[PLC] Connection Established (192.168.22.39)
[PLC] Connection established, checking Recipe status...
[Recipe] Auto-loading Recipe after PLC connection...
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
[Recipe Monitor] Registered: Heater Temperature (D100) Length=1 Type=Short
[Recipe Monitor] Registered: Cooling Water Pressure (D102) Length=2 Type=DWord
[Recipe Monitor] Registered: Conveyor Speed (D104) Length=1 Type=Short
[Recipe Monitor] Registered: Mixer RPM (D106) Length=1 Type=Short
[Recipe Monitor] Registered: Pressure Time (D110) Length=1 Type=Short
[Recipe Monitor] Registered: Holding Time (D112) Length=1 Type=Short
[Recipe Monitor] Registered: Cooling Time (D114) Length=1 Type=Short
[Recipe Monitor] Registered: Material A Dosage (D120) Length=1 Type=Short
[Recipe Monitor] Registered: Material B Dosage (D122) Length=1 Type=Short
[Recipe Monitor] Registered: Alarm Critical Temp (D200) Length=1 Type=Short
[Recipe] Monitoring started: 10 parameters registered
[Recipe] Auto-loaded and monitoring started: 10 parameters
```

## 驗證檢查

### 1. 檢查 Recipe 是否已載入
```csharp
bool hasRecipe = RecipeContext.HasActiveRecipe;
Console.WriteLine($"Has Recipe: {hasRecipe}");
```

### 2. 檢查 PLC 是否已連線
```csharp
bool isConnected = PlcContext.GlobalStatus?.CurrentManager?.IsConnected ?? false;
Console.WriteLine($"PLC Connected: {isConnected}");
```

### 3. 檢查監控是否運行
```csharp
bool isMonitoring = RecipeContext.IsMonitoring;
Console.WriteLine($"Recipe Monitoring: {isMonitoring}");
```

### 4. 檢查註冊的地址
```csharp
string addresses = RecipeContext.GenerateMonitorAddresses();
Console.WriteLine($"Monitor Addresses: {addresses}");
// 應該輸出: "D100:1,D102:2,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1"
```

## 故障排除

### 問題：監控沒有啟動

**檢查 1：Recipe 是否在 PLC 連線之前載入？**
```
如果日誌顯示 Recipe 載入在 "System initialized. Main PLC set." 之前，
說明時序錯誤。
```

**解決方法：**
- 設定 `RecipeLoader.AutoLoadOnStartup="False"`
- 或實現方案 3 的智能自動載入

**檢查 2：PLC 是否已連線？**
```csharp
if (PlcContext.GlobalStatus?.CurrentManager?.IsConnected == false)
{
    Console.WriteLine("PLC not connected!");
}
```

**檢查 3：MonitorService 是否運行？**
```csharp
if (PlcContext.GlobalStatus?.CurrentManager?.Monitor?.IsRunning == false)
{
    Console.WriteLine("Monitor service not running!");
}
```

### 問題：DWord 類型沒有正確註冊兩個暫存器

**檢查：**
```csharp
string addresses = RecipeContext.GenerateMonitorAddresses();
// D102 應該是 "D102:2"，不是 "D102:1"
```

**確認 Recipe.json：**
```json
{
  "Name": "Cooling Water Pressure",
  "Address": "D102",
  "DataType": "DWord",  ← 必須是 DWord、Float、Int 或 Int32
  "IsEnabled": true
}
```

## 建議配置

### 開發環境
```xml
<!-- 方便測試，手動控制 -->
<Controls:PlcStatus AutoConnect="False" />
<Controls:RecipeLoader AutoLoadOnStartup="False" />
```
+ 在 MainWindow.xaml.cs 實現智能自動載入（方案 3）

### 生產環境
```xml
<!-- 全自動啟動 -->
<Controls:PlcStatus AutoConnect="True" />
<Controls:RecipeLoader AutoLoadOnStartup="True" />
```
+ RecipeLoader 會在 Recipe 載入後自動啟動監控

## 總結

| 方案 | 優點 | 缺點 | 適用場景 |
|------|------|------|----------|
| **方案 1**: 延遲載入 | 簡單 | 需要手動操作 | 開發測試 |
| **方案 2**: 自動連線 | 全自動 | 啟動失敗時卡住 | 生產環境 |
| **方案 3**: 智能載入 | 靈活、可靠 | 需要寫代碼 | **推薦** |

**推薦使用方案 3**：
- ? 自動處理任何順序
- ? 支援手動和自動
- ? 最可靠的解決方案
