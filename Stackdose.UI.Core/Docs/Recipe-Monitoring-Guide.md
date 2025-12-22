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
