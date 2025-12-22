# Recipe Load 自動下載更新

## 更新摘要

**Load Recipe 按鈕現在會自動下載 Recipe 到 PLC！**

### 之前

```
1. 點擊 "Load Recipe" → 載入 JSON
2. 點擊 "Download to PLC" → 寫入 PLC
```

需要**兩個步驟**，分開操作。

### 現在

```
1. 點擊 "Load Recipe" → 載入 JSON + 自動寫入 PLC ?
```

只需**一個步驟**，自動完成！

## 使用場景

### 場景 1：PLC 已連線

```
操作：點擊 "Load Recipe"
結果：
  - Recipe 載入成功 ?
  - 自動下載到 PLC ?
  - 彈出訊息："Recipe loaded and downloaded successfully! 10 parameters written to PLC."
  - PLC 數值立即更新 ?
```

### 場景 2：PLC 未連線

```
操作：點擊 "Load Recipe"
結果：
  - Recipe 載入成功 ?
  - 彈出訊息："Recipe loaded successfully. Note: PLC is not connected."
  - 等待 PLC 連線

操作：連線 PLC
結果：
  - 自動下載到 PLC ?
  - 日誌："[Recipe] Auto-downloaded to PLC: 10 parameters written"
  - PLC 數值更新 ?
```

### 場景 3：應用程式啟動（自動載入）

```
1. 應用程式啟動
   - Recipe 自動載入（如果設定 AutoLoadOnStartup="True"）

2. 連線 PLC
   - 自動下載到 PLC ?
   - 日誌："[Recipe] Auto-loaded and downloaded: 10 parameters written to PLC"
```

## UI 變更

### RecipeLoader 控制項

**之前**：
```
[Load Recipe] [Reload] [Download to PLC]
```

**現在**：
```
[Load Recipe] [Reload]
```

**移除**：
- ? Download to PLC 按鈕（功能已整合到 Load 按鈕）

## 程式碼變更

### RecipeLoader.xaml.cs

#### LoadRecipeAsync()

```csharp
private async Task LoadRecipeAsync()
{
    // 1. 載入 Recipe JSON
    bool success = await RecipeContext.LoadRecipeAsync(RecipeFilePath, ...);
    
    if (!success) return;
    
    // 2. 檢查 PLC 是否連線
    var plcStatus = PlcContext.GlobalStatus;
    if (plcStatus?.CurrentManager?.IsConnected == true)
    {
        // 3. 自動下載到 PLC ?
        int count = await RecipeContext.DownloadRecipeToPLCAsync(plcStatus.CurrentManager);
        // 顯示成功訊息
    }
    else
    {
        // PLC 未連線，等待連線後自動下載
    }
}
```

#### ReloadRecipeAsync()

```csharp
private async Task ReloadRecipeAsync()
{
    // 1. 重新載入 Recipe
    bool success = await RecipeContext.ReloadCurrentRecipeAsync();
    
    // 2. 自動下載到 PLC ?
    if (PLC 已連線)
    {
        await RecipeContext.DownloadRecipeToPLCAsync(...);
    }
}
```

### MainWindow.xaml.cs

#### OnPlcConnectionEstablished()

```csharp
private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    if (!RecipeContext.HasActiveRecipe)
    {
        // 自動載入 Recipe
        await RecipeContext.LoadRecipeAsync("Recipe.json", ...);
    }
    
    // 自動下載到 PLC ?
    int count = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
}
```

## 日誌範例

### 成功載入並下載

```
[Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (10 parameters)
[Recipe] Downloading Recipe to PLC: Standard Process Recipe A v1.2.0
[Recipe Download] Heater Temperature (D100) = 180 °C
[Recipe Download] Cooling Water Pressure (D102) = 3.5 bar
[Recipe Download] Conveyor Speed (D104) = 120 mm/s
[Recipe Download] Mixer RPM (D106) = 450 RPM
[Recipe Download] Pressure Time (D110) = 30 sec
[Recipe Download] Holding Time (D112) = 120 sec
[Recipe Download] Cooling Time (D114) = 90 sec
[Recipe Download] Material A Dosage (D120) = 250 g
[Recipe Download] Material B Dosage (D122) = 150 g
[Recipe Download] Alarm Critical Temp (D200) = 200 °C
[Recipe] Download completed successfully: 10/10 parameters written
```

### PLC 連線時自動下載

```
[PLC] Connection established, checking Recipe status...
[Recipe] Auto-downloaded to PLC: 10 parameters written
```

## 驗證方法

### 1. 檢查 PLC 數值

**之前（只載入，不下載）**：
```
Recipe: D100 = "180"
PLC:    D100 = 100  ?（沒變）
```

**現在（載入 + 自動下載）**：
```
Recipe: D100 = "180"
PLC:    D100 = 180  ?（已更新）
```

### 2. 檢查日誌

查找以下日誌：
```
[Recipe] Download completed successfully: N/N parameters written
```

### 3. 檢查 PlcLabel 顯示

如果有使用 PlcLabel 監控 D100：
```xaml
<Controls:PlcLabel Address="D100" Label="Heater Temperature" />
```

應該會顯示 **180**（Recipe 設定值），而不是原始的 100。

## 優點

### 1. 簡化操作

**之前**：
```
Load → Download → 確認 → 完成
```

**現在**：
```
Load → 完成 ?
```

### 2. 避免忘記下載

使用者不會忘記點擊 Download 按鈕，因為已經自動執行。

### 3. 符合直覺

"Load Recipe" 應該是 **載入並應用**，而不只是讀取檔案。

### 4. 自動處理時序

無論 Recipe 先載入還是 PLC 先連線，都會正確處理。

## 注意事項

### 1. 權限控制

Load Recipe 按鈕仍然需要 `Instructor` 權限（或更高）。

### 2. 審計追蹤

每次下載都會記錄到審計追蹤資料庫。

### 3. 錯誤處理

如果下載失敗，會顯示警告訊息但不會中斷流程。

## 測試建議

### 測試案例 1：正常流程

```
1. 啟動應用程式
2. 連線 PLC
3. 點擊 "Load Recipe"
4. 驗證：PLC 數值已更新為 Recipe 設定值 ?
```

### 測試案例 2：PLC 未連線

```
1. 啟動應用程式（不連線 PLC）
2. 點擊 "Load Recipe"
3. 驗證：顯示 "Recipe loaded (PLC not connected)"
4. 連線 PLC
5. 驗證：自動下載，PLC 數值已更新 ?
```

### 測試案例 3：自動載入

```
1. 設定 RecipeLoader AutoLoadOnStartup="True"
2. 啟動應用程式
3. Recipe 自動載入
4. 連線 PLC
5. 驗證：自動下載，PLC 數值已更新 ?
```

## 總結

**核心改變**：Load Recipe = 載入 + 自動下載

這個改變讓 Recipe 系統更加**直觀**、**簡單**、**自動化**！

**一個按鈕，完成所有事情！** ?
