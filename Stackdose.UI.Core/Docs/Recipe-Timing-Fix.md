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

## 統一監控架構 ?

Recipe 監控遵循 **統一監控模式**，由 `PlcStatus` 統一管理：

```
PlcStatus 連線成功
  ↓
自動調用 RecipeContext.GenerateMonitorAddresses()
  ↓
PlcStatus.RegisterMonitors() 統一註冊
  ↓
完成 ?
```

**RecipeContext 只負責**：
- ? 提供 `GenerateMonitorAddresses()` 方法
- ? 載入和管理 Recipe 配方
- ? 不負責啟動監控（由 PlcStatus 統一處理）

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
   → PlcStatus 調用 RecipeContext.GenerateMonitorAddresses()
   → 如果 Recipe 已載入，自動註冊監控 ?
3. 手動點擊 RecipeLoader 的 Load 按鈕
4. Recipe 載入成功
5. 下次 PLC 重新連線時，監控會自動註冊
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
2. Recipe 自動載入 ?
3. PLC 自動連線 ?
4. PlcStatus 調用 RecipeContext.GenerateMonitorAddresses()
5. 監控自動註冊 ?
```

### 方案 3：智能自動載入（推薦）?

在 `MainWindow.xaml.cs` 中訂閱 PLC 連線事件，當 PLC 連線成功時自動載入 Recipe。

**MainWindow.xaml.cs**
```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ? 訂閱 PLC 連線成功事件
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
}

private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // 如果 Recipe 還沒載入，自動載入
    if (!RecipeContext.HasActiveRecipe)
    {
        ComplianceContext.LogSystem(
            "[Recipe] Auto-loading Recipe after PLC connection...",
            LogLevel.Info,
            showInUi: true
        );

        bool success = await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
        
        if (success && RecipeContext.CurrentRecipe != null)
        {
            ComplianceContext.LogSystem(
                $"[Recipe] Auto-loaded successfully: {RecipeContext.CurrentRecipe.RecipeName}",
                LogLevel.Success,
                showInUi: true
            );
        }
    }
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
- ? PlcStatus 統一管理監控註冊

**操作流程：**
```
情況 A：PLC 先連線
1. 點擊 PlcStatus 連線 PLC
2. 觸發 ConnectionEstablished 事件
3. 自動載入 Recipe
4. PlcStatus 已在連線時調用過 GenerateMonitorAddresses()（返回空）
5. 需要重新連線以註冊 Recipe 監控

情況 B：Recipe 先載入（手動點擊 Load）
1. 點擊 RecipeLoader 的 Load 按鈕
2. Recipe 載入成功
3. 點擊 PlcStatus 連線 PLC
4. PlcStatus 調用 RecipeContext.GenerateMonitorAddresses()
5. 自動註冊 Recipe 監控 ?

情況 C：使用自動載入（方案 3）
1. 點擊 PlcStatus 連線 PLC
2. 觸發 ConnectionEstablished 事件
3. MainWindow 自動載入 Recipe
4. 下次 PLC 重新連線時，自動註冊監控 ?
```

## 重要說明

### PlcStatus 連線時的監控註冊

PlcStatus 在 **連線成功時** 會自動調用：

```csharp
// PlcStatus.xaml.cs ConnectAsync() 方法中
string recipeAddresses = RecipeContext.GenerateMonitorAddresses();
if (!string.IsNullOrWhiteSpace(recipeAddresses))
{
    RegisterMonitors(recipeAddresses);
}
```

**這意味著**：
- ? Recipe 必須在 PLC 連線時已經載入
- ? Recipe 在 PLC 連線後載入，監控不會自動註冊
- ? 需要重新連線 PLC 才能註冊新載入的 Recipe

### 最佳實踐

**開發環境**（方案 3 推薦）：
```xml
<Controls:PlcStatus AutoConnect="False" />
<Controls:RecipeLoader AutoLoadOnStartup="False" />
```
```csharp
// MainWindow.xaml.cs
MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;

private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    if (!RecipeContext.HasActiveRecipe)
    {
        await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
        
        // ? 重要：Recipe 剛載入，需要重新連線以註冊監控
        // 或者手動觸發監控註冊
        await plcManager.DisconnectAsync();
        await Task.Delay(500);
        await plcManager.InitializeAsync(IpAddress, Port, ScanInterval);
    }
}
```

**生產環境**（方案 2 推薦）：
```xml
<Controls:PlcStatus AutoConnect="True" />
<Controls:RecipeLoader AutoLoadOnStartup="True" />
```

## 日誌輸出（正確流程）

```
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
[PLC] Connecting to PLC (192.168.22.39:3000)...
[PLC] Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:1,D102:2,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
System initialized. Main PLC set.
```

**關鍵**：`[AutoRegister] Recipe:` 行應該顯示所有 Recipe 監控地址。

## 故障排除

### 問題：Recipe 載入了，但沒有看到 [AutoRegister] Recipe: 日誌

**原因**：Recipe 在 PLC 連線後才載入。

**解決方法**：
1. 重新連線 PLC（點擊 PlcStatus 斷線再連線）
2. 或使用方案 2（全自動啟動）
3. 或使用方案 3（智能自動載入 + 重新連線）

### 問題：如何確認 Recipe 監控已註冊？

**檢查 1：日誌輸出**
```
[AutoRegister] Recipe: D100:1,D102:2,D104:1,...
```

**檢查 2：程式檢查**
```csharp
string addresses = RecipeContext.GenerateMonitorAddresses();
Console.WriteLine($"Recipe Monitor Addresses: {addresses}");
// 應該輸出: "D100:1,D102:2,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1"
```

**檢查 3：PlcMonitorService**
```csharp
bool isMonitoring = PlcContext.GlobalStatus?.CurrentManager?.Monitor?.IsRunning ?? false;
Console.WriteLine($"Monitor Running: {isMonitoring}");
```

## 總結

| 方案 | 優點 | 缺點 | 適用場景 |
|------|------|------|----------|
| **方案 1**: 延遲載入 | 簡單 | 需要手動操作 | 開發測試 |
| **方案 2**: 自動連線 | 全自動 | 啟動失敗時卡住 | 生產環境 |
| **方案 3**: 智能載入 | 靈活、可靠 | 需要寫代碼 | **推薦** |

**核心原則**：
- ? Recipe 必須在 PLC 連線時已載入
- ? PlcStatus 統一管理所有監控註冊
- ? RecipeContext 只提供 GenerateMonitorAddresses()
- ? 不要在其他地方手動啟動監控
