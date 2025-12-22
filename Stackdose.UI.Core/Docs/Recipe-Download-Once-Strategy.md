# Recipe 下載策略：只在第一次連線時下載

## 設計理念

**PLC 資料暫存器（D 暫存器）在斷線後會保持數值**，因此不需要在每次重連時重複下載 Recipe。

## 下載策略

### 自動下載（只執行一次）

```
1. 應用程式啟動
   ↓
2. PLC 第一次連線
   ↓
3. 自動下載 Recipe 到 PLC ?
   _recipeDownloadedToPLC = true
   ↓
4. PLC 斷線
   ↓
5. PLC 重連
   ↓
6. 檢查 _recipeDownloadedToPLC == true
   ↓
7. 跳過下載（PLC 數值已保留）?
```

### 手動下載（任何時候都可以）

```
使用者點擊 RecipeLoader 的 Load 或 Reload 按鈕
   ↓
Recipe 重新載入並下載到 PLC ?
   ↓
_recipeDownloadedToPLC 保持 true
   ↓
下次重連時依然不會自動下載
```

## 實現細節

### MainWindow.xaml.cs

#### 1. 添加標記

```csharp
public partial class MainWindow : Window
{
    /// <summary>
    /// 標記 Recipe 是否已經下載到 PLC（避免重連時重複下載）
    /// </summary>
    private bool _recipeDownloadedToPLC = false;
}
```

#### 2. 檢查標記

```csharp
private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // ? 檢查是否已經下載過 Recipe
    if (_recipeDownloadedToPLC)
    {
        ComplianceContext.LogSystem(
            "[Recipe] Recipe already downloaded to PLC, skipping re-download on reconnection.",
            LogLevel.Info,
            showInUi: true
        );
        return; // ? 跳過下載
    }

    // 第一次連線，執行下載
    if (!RecipeContext.HasActiveRecipe)
    {
        bool success = await RecipeContext.LoadRecipeAsync("Recipe.json", isAutoLoad: true);
        
        if (success && RecipeContext.CurrentRecipe != null)
        {
            int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
            
            if (downloadCount > 0)
            {
                _recipeDownloadedToPLC = true; // ? 標記已下載
            }
        }
    }
    else
    {
        int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
        
        if (downloadCount > 0)
        {
            _recipeDownloadedToPLC = true; // ? 標記已下載
        }
    }
}
```

## 日誌輸出

### 第一次連線（會下載）

```
[Application Start]
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)

[User clicks Connect]
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Recipe: D100:2,D103:1,D104:1,...
[PlcStatus] Triggering ConnectionEstablished event...
[MainWindow] OnPlcConnectionEstablished called!
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already loaded, downloading to PLC...
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
[Recipe Download] Cooling Water Pressure (D103) = 35 bar
...
[Recipe] Download completed successfully: 10/10 parameters written
[Recipe] Auto-downloaded to PLC: 10 parameters written
```

### 重連（不會下載）

```
[User clicks Disconnect]
[PLC] Disconnecting...
[PLC] Disconnected

[User clicks Connect]
Connecting to PLC (192.168.22.39:3000)...
PLC Connection Established (192.168.22.39)
[AutoRegister] Recipe: D100:2,D103:1,D104:1,...
[PlcStatus] Triggering ConnectionEstablished event...
[MainWindow] OnPlcConnectionEstablished called!
[PLC] Connection established, checking Recipe status...
[Recipe] Recipe already downloaded to PLC, skipping re-download on reconnection.  ← ? 跳過下載
```

### 手動載入（會下載）

```
[User clicks RecipeLoader Load button]
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 200000 °C
...
[Recipe] Download completed successfully: 10/10 parameters written
Recipe 載入並下載成功: 10 個參數  ← ? 手動下載成功
```

## 使用場景

### 場景 1：正常啟動

```
1. 啟動應用程式
2. Recipe 自動載入
3. 連線 PLC
4. Recipe 自動下載到 PLC ?
5. 開始生產
```

### 場景 2：PLC 斷線重連

```
1. PLC 斷線（網路問題、PLC 重啟等）
2. PLC 重連
3. Recipe 不會重新下載（PLC 數值保留）?
4. 繼續生產
```

### 場景 3：需要更新 Recipe

```
1. 修改 Recipe.json
2. 點擊 RecipeLoader 的 Reload 按鈕
3. Recipe 重新載入並下載到 PLC ?
4. PLC 數值更新
```

### 場景 4：應用程式重啟

```
1. 關閉應用程式
2. 重新啟動應用程式
3. _recipeDownloadedToPLC 重置為 false
4. 連線 PLC
5. Recipe 自動下載（因為應用程式重啟）?
```

## 優點

### 1. 減少不必要的通訊

- ? 避免每次重連都下載 Recipe
- ? 降低 PLC 通訊負擔
- ? 提高連線速度

### 2. 保護 PLC 數值

- ? PLC 斷線後，D 暫存器數值保留
- ? 重連後不會覆蓋現有數值
- ? 避免誤操作導致參數被重置

### 3. 靈活性

- ? 需要更新時，可以手動點擊 Load/Reload
- ? 應用程式重啟後，會重新評估是否需要下載
- ? 支援多種使用場景

## 何時會重新下載？

### 會重新下載的情況

| 情況 | 說明 | 原因 |
|------|------|------|
| **第一次連線** | 應用程式啟動後第一次連線 PLC | `_recipeDownloadedToPLC = false` |
| **手動 Load** | 點擊 RecipeLoader 的 Load 按鈕 | 明確的手動操作 |
| **手動 Reload** | 點擊 RecipeLoader 的 Reload 按鈕 | 明確的手動操作 |
| **應用程式重啟** | 關閉並重新啟動應用程式 | 標記重置 |

### 不會重新下載的情況

| 情況 | 說明 | 原因 |
|------|------|------|
| **重連** | PLC 斷線後重新連線 | `_recipeDownloadedToPLC = true` |
| **看門狗重連** | 自動重連機制觸發 | 同上 |
| **多次重連** | 連續斷線重連多次 | 同上 |

## 進階配置（可選）

如果您希望更靈活地控制下載行為，可以添加配置選項：

### MainWindow 中添加配置

```csharp
/// <summary>
/// 下載策略
/// </summary>
public enum RecipeDownloadStrategy
{
    /// <summary>
    /// 只在第一次連線時下載
    /// </summary>
    OnceOnly,
    
    /// <summary>
    /// 每次連線都下載
    /// </summary>
    Always,
    
    /// <summary>
    /// 從不自動下載（只能手動）
    /// </summary>
    Manual
}

private RecipeDownloadStrategy _downloadStrategy = RecipeDownloadStrategy.OnceOnly;
```

### 根據策略決定是否下載

```csharp
private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    switch (_downloadStrategy)
    {
        case RecipeDownloadStrategy.OnceOnly:
            if (_recipeDownloadedToPLC) return; // 跳過
            break;
            
        case RecipeDownloadStrategy.Always:
            // 每次都下載
            break;
            
        case RecipeDownloadStrategy.Manual:
            // 從不自動下載
            return;
    }
    
    // 執行下載...
}
```

## 故障排除

### 問題：PLC 數值沒有更新

**可能原因**：
1. Recipe 已經下載過，重連時跳過了
2. PLC 斷線前的數值保留在記憶體中

**解決方法**：
- 點擊 RecipeLoader 的 **Reload** 按鈕，強制重新下載

### 問題：想要每次重連都下載

**解決方法**：
移除標記檢查，改為：

```csharp
private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // 移除這段檢查
    // if (_recipeDownloadedToPLC) return;
    
    // 每次都下載
    int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
}
```

### 問題：應用程式重啟後，PLC 數值被覆蓋

**說明**：這是正常行為，因為應用程式重啟後 `_recipeDownloadedToPLC` 重置為 `false`。

**解決方法（如果不希望這樣）**：
將標記保存到檔案或資料庫，應用程式重啟時讀取。

## 總結

### 當前設計

- ? **第一次連線**：自動下載
- ? **重連**：不下載（PLC 數值保留）
- ? **手動 Load/Reload**：下載
- ? **應用程式重啟**：重新評估

### 優點

- 減少不必要的 PLC 通訊
- 保護現有 PLC 數值
- 支援手動更新
- 簡單且有效

### 適用場景

- 生產環境（PLC 需要保持穩定）
- 網路不穩定環境（頻繁重連）
- 需要減少 PLC 負擔的場景

**這個設計符合工業控制系統的最佳實踐！** ??
