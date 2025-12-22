# Recipe Monitoring Guide

## 概述

Recipe 監控功能已整合到 RecipeContext 中，支援自動監控 Recipe 中所有啟用的參數。

## 主要功能

### 1. 數據類型更新

Recipe.json 中的 DataType 命名已更新：

| 舊名稱 | 新名稱 | 佔用暫存器數量 | 說明 |
|--------|--------|----------------|------|
| Int16  | Short 或 Word | 1 | 16位元整數 |
| Float  | DWord | 2 | 32位元浮點數（佔用連續兩個暫存器）|
| Int32  | Int 或 DWord | 2 | 32位元整數（佔用連續兩個暫存器）|

### 2. 自動監控註冊

當 PLC 連線成功時，系統會自動：

1. **讀取 Recipe 配置**：從 `RecipeContext.CurrentRecipe` 取得所有啟用的參數
2. **計算暫存器數量**：
   - Short/Word 類型：1 個暫存器
   - DWord/Int/Float 類型：2 個連續暫存器
3. **註冊到 PlcMonitorService**：自動將所有地址註冊到監控服務
4. **啟動監控**：開始即時監控數值變化

### 3. Recipe 監控 API

#### StartMonitoring

```csharp
// 手動啟動 Recipe 監控
int registeredCount = RecipeContext.StartMonitoring(
    plcManager: plcStatus.CurrentManager,
    autoStart: true  // 是否自動啟動監控服務
);
```

**功能**：
- 解析 Recipe 中所有啟用項目的地址
- 根據 DataType 決定監控長度（1或2個暫存器）
- 註冊到 PlcMonitorService
- 返回成功註冊的參數數量

#### StopMonitoring

```csharp
// 停止 Recipe 監控
RecipeContext.StopMonitoring();
```

**功能**：
- 標記監控狀態為停止
- 觸發 MonitoringStopped 事件
- 不會停止整個 Monitor 服務（因為其他控制項可能也在使用）

#### GenerateMonitorAddresses

```csharp
// 生成監控地址配置字串
string addresses = RecipeContext.GenerateMonitorAddresses();
// 範例輸出: "D100:1,D102:2,D104:1,D106:1,..."
```

**格式**：`"地址:長度,地址:長度,..."`

### 4. 自動流程

#### 流程 A：Recipe 先載入，PLC 後連線

```
1. 使用者載入 Recipe
   ↓
2. RecipeContext.CurrentRecipe 已設定
   ↓
3. PLC 連線成功
   ↓
4. PlcStatus 自動調用 RecipeContext.GenerateMonitorAddresses()
   ↓
5. 自動註冊所有 Recipe 參數到 Monitor
```

#### 流程 B：PLC 先連線，Recipe 後載入

```
1. PLC 連線成功
   ↓
2. 使用者載入 Recipe
   ↓
3. RecipeLoader 的 OnRecipeLoaded 事件觸發
   ↓
4. 檢查 PlcContext.GlobalStatus 是否已連線
   ↓
5. 自動調用 RecipeContext.StartMonitoring()
   ↓
6. 註冊所有 Recipe 參數到 Monitor
```

### 5. 事件通知

```csharp
// 訂閱監控啟動事件
RecipeContext.MonitoringStarted += (sender, recipe) =>
{
    Console.WriteLine($"Recipe monitoring started: {recipe.RecipeName}");
};

// 訂閱監控停止事件
RecipeContext.MonitoringStopped += (sender, args) =>
{
    Console.WriteLine("Recipe monitoring stopped");
};
```

## 使用範例

### Recipe.json 範例

```json
{
  "RecipeId": "RECIPE-001",
  "RecipeName": "標準製程 A",
  "Items": [
    {
      "Name": "加熱溫度",
      "Address": "D100",
      "Value": "180",
      "DataType": "Short",
      "IsEnabled": true
    },
    {
      "Name": "冷卻水壓力",
      "Address": "D102",
      "Value": "3.5",
      "DataType": "DWord",
      "IsEnabled": true
    },
    {
      "Name": "輸送帶速度",
      "Address": "D104",
      "Value": "120",
      "DataType": "Short",
      "IsEnabled": true
    }
  ]
}
```

### XAML 配置

```xml
<!-- PLC 連線狀態 -->
<PlcStatus 
    IpAddress="192.168.1.10"
    Port="502"
    AutoConnect="True"
    ScanInterval="150"
    IsGlobal="True" />

<!-- Recipe 載入器 -->
<RecipeLoader 
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="True"
    ShowDetails="True" />
```

### 手動控制範例

```csharp
// 在 PLC 連線成功後手動啟動 Recipe 監控
private void OnPlcConnected(IPlcManager plcManager)
{
    if (RecipeContext.HasActiveRecipe)
    {
        int count = RecipeContext.StartMonitoring(plcManager);
        MessageBox.Show($"開始監控 {count} 個 Recipe 參數");
    }
}

// 停止監控
private void StopRecipeMonitoring()
{
    RecipeContext.StopMonitoring();
}
```

## DWord 類型處理

### 地址分配規則

當 Recipe 項目的 DataType 為 `DWord`, `Float`, `Int`, 或 `Int32` 時：

1. **佔用兩個連續暫存器**
   - 例如：`D102` 會佔用 D102 和 D103
   
2. **自動註冊長度為 2**
   ```csharp
   // RecipeContext 內部處理
   if (dataType == "DWord" || dataType == "Float" || ...)
   {
       length = 2;  // 註冊兩個暫存器
   }
   ```

3. **避免地址衝突**
   - ? 正確：D100(Short), D102(DWord), D104(Short)
   - ? 錯誤：D100(Short), D101(DWord) ← D101和D102衝突

### 建議的地址配置

```
D100: Short (佔用 D100)
D102: DWord (佔用 D102, D103)
D104: Short (佔用 D104)
D106: Short (佔用 D106)
D110: DWord (佔用 D110, D111)
```

## 日誌輸出

監控註冊過程會記錄到 ComplianceContext：

```
[Recipe Monitor] Registered: 加熱溫度 (D100) Length=1 Type=Short
[Recipe Monitor] Registered: 冷卻水壓力 (D102) Length=2 Type=DWord
[Recipe Monitor] Registered: 輸送帶速度 (D104) Length=1 Type=Short
[Recipe] Monitoring started: 3 parameters registered
```

## 注意事項

1. **地址格式**：
   - 只支援 `D` 和 `R` 裝置（D100, R200）
   - 不支援 `M` 裝置（M100）← Recipe 不應包含 M 地址

2. **DWord 對齊**：
   - DWord 類型建議從偶數地址開始（D100, D102, D104...）
   - 避免地址重疊

3. **監控順序**：
   - 無論 Recipe 先載入還是 PLC 先連線，系統都會自動處理
   - 兩個流程都會正確註冊監控

4. **性能考慮**：
   - PlcMonitorService 會自動合併連續的地址範圍
   - 減少 PLC 讀取次數，提升效率

## 除錯檢查

如果監控未啟動，請檢查：

```csharp
// 1. Recipe 是否已載入
bool hasRecipe = RecipeContext.HasActiveRecipe;
Console.WriteLine($"Has Recipe: {hasRecipe}");

// 2. PLC 是否已連線
bool isConnected = PlcContext.GlobalStatus?.CurrentManager?.IsConnected ?? false;
Console.WriteLine($"PLC Connected: {isConnected}");

// 3. Monitor 是否運行
bool isMonitoring = PlcContext.GlobalStatus?.CurrentManager?.Monitor?.IsRunning ?? false;
Console.WriteLine($"Monitor Running: {isMonitoring}");

// 4. 生成的監控地址
string addresses = RecipeContext.GenerateMonitorAddresses();
Console.WriteLine($"Monitor Addresses: {addresses}");

// 5. 監控狀態
bool recipeMonitoring = RecipeContext.IsMonitoring;
Console.WriteLine($"Recipe Monitoring: {recipeMonitoring}");
```

## 更新日誌

- **2024-01-XX**: 初始版本
  - 新增 Recipe 監控功能
  - 支援 DWord 類型（佔用兩個暫存器）
  - 自動註冊機制
  - DataType 命名更新（Int16→Short, Float→DWord）

# Recipe Monitoring Architecture

## 統一監控架構 ?

Recipe 監控遵循 **統一監控模式**，由 `PlcStatus` 統一管理所有監控註冊。

### 架構設計

```
PlcStatus 連線成功
  ↓
自動調用各個 Context 的 GenerateMonitorAddresses()
  - SensorContext.GenerateMonitorAddresses()
  - PlcLabelContext.GenerateMonitorAddresses()
  - PlcEventContext.GenerateMonitorAddresses()
  - RecipeContext.GenerateMonitorAddresses()  ?
  ↓
PlcStatus.RegisterMonitors() 統一註冊
  ↓
PlcMonitorService 開始監控
```

### RecipeContext 的職責

RecipeContext **只負責**：
- ? 載入和管理 Recipe 配方
- ? 提供 `GenerateMonitorAddresses()` 方法
- ? Recipe 資料驗證

RecipeContext **不負責**：
- ? 啟動/停止監控（由 PlcStatus 統一處理）
- ? 註冊監控地址（由 PlcStatus 統一處理）
- ? 管理 PlcManager 實例

## Recipe 監控流程

### 自動流程（推薦）

```
1. 應用程式啟動
   ↓
2. PLC 連線成功
   ↓
3. PlcStatus 自動調用 RecipeContext.GenerateMonitorAddresses()
   ↓
4. 如果 Recipe 已載入，返回監控地址清單
   如果 Recipe 未載入，返回空字串
   ↓
5. PlcStatus 統一註冊所有地址
   ↓
6. 監控自動啟動 ?
```

### 延遲載入 Recipe

如果 Recipe 在 PLC 連線後才載入：

```
1. PLC 已連線
   ↓
2. PlcStatus 調用 RecipeContext.GenerateMonitorAddresses()
   → 返回空字串（Recipe 未載入）
   ↓
3. 使用者或程式載入 Recipe
   ↓
4. Recipe 載入成功
   ↓
5. PlcStatus 需要重新掃描或重新連線才能註冊 Recipe 監控
```

**解決方案**：在 MainWindow 中訂閱 PLC ConnectionEstablished 事件，自動載入 Recipe

```csharp
MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;

private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    if (!RecipeContext.HasActiveRecipe)
    {
        await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
    }
}
```

## GenerateMonitorAddresses() 方法

### 功能

生成 Recipe 監控位址配置字串，供 PlcStatus 自動註冊使用。

### 格式

```
"地址:長度,地址:長度,..."
```

### 範例

**Recipe.json**
```json
{
  "Items": [
    { "Address": "D100", "DataType": "Short", "IsEnabled": true },
    { "Address": "D102", "DataType": "DWord", "IsEnabled": true },
    { "Address": "D104", "DataType": "Short", "IsEnabled": true }
  ]
}
```

**輸出**
```
"D100:1,D102:2,D104:1"
```

### DataType 對應長度

| DataType | 暫存器數量 | 說明 |
|----------|-----------|------|
| Short, Word | 1 | 16位元 |
| DWord, Float, Int, Int32 | 2 | 32位元（佔用連續兩個暫存器）|

### 實現

```csharp
public static string GenerateMonitorAddresses()
{
    if (CurrentRecipe == null || !CurrentRecipe.Items.Any())
        return string.Empty;

    var addresses = new List<string>();

    foreach (var item in CurrentRecipe.Items.Where(x => x.IsEnabled))
    {
        var match = Regex.Match(item.Address, @"^([DR])(\d+)$");
        if (!match.Success)
            continue;

        int length = 1;

        // DWord/Float 需要兩個連續暫存器
        if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Float", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
            item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
        {
            length = 2;
        }

        addresses.Add($"{item.Address}:{length}");
    }

    return string.Join(",", addresses);
}
```

## 日誌輸出

### 正確的監控註冊流程

```
[PLC] Connecting to PLC (192.168.22.39:3000)...
[PLC] Connection Established (192.168.22.39)
[AutoRegister] Sensor: D10:1,R2000:1,R2002:1
[AutoRegister] PlcLabel: D10:1,D11:1,M237:1,R2000:1,R2002:1
[AutoRegister] PlcEvent: M237:1,M238:1
[AutoRegister] Recipe: D100:1,D102:2,D104:1,D106:1,D110:1,D112:1,D114:1,D120:1,D122:1,D200:1
System initialized. Main PLC set.
```

**注意**：Recipe 監控地址應該在 `[AutoRegister] Recipe:` 行中顯示。

## 常見問題

### Q: Recipe 載入後監控沒有啟動？

**A**: 檢查 Recipe 是否在 PLC 連線時已經載入：

```csharp
// 檢查時序
bool hasRecipe = RecipeContext.HasActiveRecipe;
bool isConnected = PlcContext.GlobalStatus?.CurrentManager?.IsConnected ?? false;

Console.WriteLine($"Has Recipe: {hasRecipe}");
Console.WriteLine($"PLC Connected: {isConnected}");
```

**解決方案**：
1. 確保 Recipe 在 PLC 連線前載入，或
2. 在 PLC 連線後自動載入 Recipe（見上方程式碼範例）

### Q: 為什麼不能直接調用 StartMonitoring？

**A**: 統一監控架構的設計原則：
- ? 所有監控由 PlcStatus 統一管理
- ? 避免重複註冊
- ? 簡化程式碼
- ? 易於維護

如果每個 Context 都自己啟動監控：
- ? 重複註冊相同地址
- ? 難以追蹤監控狀態
- ? 增加複雜度

### Q: 如何確認 Recipe 監控已註冊？

**A**: 檢查日誌輸出：

```
[AutoRegister] Recipe: D100:1,D102:2,D104:1,...
```

或者在程式中檢查：

```csharp
string addresses = RecipeContext.GenerateMonitorAddresses();
Console.WriteLine($"Recipe Monitor Addresses: {addresses}");
```

### Q: DWord 類型沒有正確註冊兩個暫存器？

**A**: 檢查：

1. **Recipe.json 中的 DataType**
   ```json
   {
     "Address": "D102",
     "DataType": "DWord"  ← 必須是 DWord、Float、Int 或 Int32
   }
   ```

2. **生成的地址字串**
   ```csharp
   // D102 應該是 "D102:2"，不是 "D102:1"
   string addresses = RecipeContext.GenerateMonitorAddresses();
   ```

## 配置範例

### MainWindow.xaml

```xml
<!-- PLC 連線狀態 -->
<Controls:PlcStatus x:Name="MainPlc"
    IpAddress="192.168.22.39"
    Port="3000"
    AutoConnect="False"
    ScanInterval="120"
    IsGlobal="True" />

<!-- Recipe 載入器 -->
<Controls:RecipeLoader 
    RecipeFilePath="Recipe.json"
    AutoLoadOnStartup="False"
    RequiredAccessLevel="Instructor"
    ShowDetails="True"/>
```

### MainWindow.xaml.cs

```csharp
public MainWindow()
{
    InitializeComponent();
    
    // ? 訂閱 PLC 連線成功事件，自動載入 Recipe
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
}

private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // 如果 Recipe 還沒載入，自動載入
    if (!RecipeContext.HasActiveRecipe)
    {
        await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
    }
}
```

## 總結

| 項目 | 負責單位 | 說明 |
|------|---------|------|
| 監控註冊 | PlcStatus | 統一管理所有監控註冊 |
| 地址生成 | RecipeContext | 提供 GenerateMonitorAddresses() |
| Recipe 載入 | RecipeContext | LoadRecipeAsync() |
| 監控服務 | PlcMonitorService | 執行實際監控工作 |

**設計原則**：
- ? 單一職責：每個組件只負責自己的工作
- ? 統一管理：所有監控由 PlcStatus 統一管理
- ? 自動化：PLC 連線時自動註冊所有監控
- ? 簡單化：避免重複程式碼和複雜邏輯
