# WpfApp1\MainWindow.xaml.cs 程式碼清理報告

## ?? 已完成的清理

### 1. 刪除未使用的方法（共 3 個，約 100 行程式碼）

#### ? `OnPlcConnectionEstablished` (82 行)
- **原因：** 事件訂閱被註解掉 `// MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;`
- **影響：** Recipe 自動下載功能不會執行
- **建議：** 如果未來需要自動下載 Recipe，應：
  1. 取消註解事件訂閱
  2. 在 `InitializeRecipeSystemAsync` 中訂閱 PLC 連線事件

#### ? `OnRecipeLoaded` (15 行)
- **原因：** 沒有訂閱 `RecipeContext.RecipeLoaded` 事件
- **影響：** Recipe 載入成功時不會有任何通知
- **建議：** 如果需要監聽 Recipe 載入，應在建構函數加入：
```csharp
RecipeContext.RecipeLoaded += OnRecipeLoaded;
```

#### ? `OnRecipeLoadFailed` (13 行)
- **原因：** 沒有訂閱 `RecipeContext.RecipeLoadFailed` 事件
- **影響：** Recipe 載入失敗時不會有任何通知
- **建議：** 如果需要監聽 Recipe 載入失敗，應在建構函數加入：
```csharp
RecipeContext.RecipeLoadFailed += OnRecipeLoadFailed;
```

---

### 2. 刪除未使用的欄位（1 個）

#### ? `_recipeDownloadedToPLC`
```csharp
private bool _recipeDownloadedToPLC = false;
```
- **原因：** 只在已刪除的 `OnPlcConnectionEstablished` 中使用
- **影響：** 無任何影響

---

### 3. 移除自動批次寫入測試

#### ?? 從 `#if DEBUG` 改為完全註解
```csharp
// ?? 測試：5 秒後自動觸發批次寫入（移除，改為手動測試）
#if DEBUG
// Task.Run(async () =>
// {
//     await Task.Delay(5000);
//     ...
// });
#endif
```
- **原因：** 每次啟動都自動測試會產生不必要的日誌
- **替代方案：** 使用 "Test Batch Write (500 logs)" 按鈕手動測試

---

## ?? 清理前後對比

| 項目 | 清理前 | 清理後 | 減少 |
|------|--------|--------|------|
| **總行數** | ~450 行 | ~350 行 | **~100 行** |
| **未使用方法** | 3 個 | 0 個 | ? |
| **未使用欄位** | 1 個 | 0 個 | ? |
| **註解程式碼** | 15 行 | 20 行 | ?5 行 |

---

## ? 保留的功能（完全正常運作）

### 核心功能
- ? ComplianceContext 初始化
- ? SecurityContext 快速登入（Admin）
- ? MainViewModel 綁定
- ? 登入/登出事件訂閱
- ? Recipe 系統初始化（預設載入 Recipe1.json）
- ? 視窗標題更新

### 測試功能
- ? 權限測試按鈕（Operator/Instructor/Supervisor/Admin）
- ? 批次寫入測試按鈕（手動測試）
- ? 手動刷新按鈕
- ? 統計資訊按鈕
- ? 製程開始按鈕

---

## ?? 可能需要復原的功能

### 1. Recipe 自動下載到 PLC

如果未來需要啟用 Recipe 自動下載功能，請：

#### 步驟 1：在建構函數中訂閱事件
```csharp
public MainWindow()
{
    // ...其他初始化

    // ? 訂閱 PLC 連線成功事件
    MainPlc.ConnectionEstablished += OnPlcConnectionEstablished;
}
```

#### 步驟 2：加回 `_recipeDownloadedToPLC` 欄位
```csharp
private bool _recipeDownloadedToPLC = false;
```

#### 步驟 3：加回 `OnPlcConnectionEstablished` 方法
```csharp
private async void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    // 檢查是否已下載
    if (_recipeDownloadedToPLC)
    {
        ComplianceContext.LogSystem(
            "[Recipe] Already downloaded, skipping.",
            LogLevel.Info, showInUi: true);
        return;
    }

    // 下載 Recipe
    int count = await RecipeContext.DownloadRecipeToPLCAsync(plcManager);
    if (count > 0)
    {
        _recipeDownloadedToPLC = true;
        ComplianceContext.LogSystem(
            $"[Recipe] Downloaded {count} parameters",
            LogLevel.Success, showInUi: true);
    }
}
```

---

### 2. Recipe 載入事件監聽

如果需要監聽 Recipe 載入成功/失敗，請：

#### 在建構函數中訂閱
```csharp
RecipeContext.RecipeLoaded += OnRecipeLoaded;
RecipeContext.RecipeLoadFailed += OnRecipeLoadFailed;
```

#### 加回事件處理方法
```csharp
private void OnRecipeLoaded(object? sender, Recipe recipe)
{
    Dispatcher.Invoke(() =>
    {
        ComplianceContext.LogSystem(
            $"[Recipe] {recipe.RecipeName} loaded successfully",
            LogLevel.Success, showInUi: true);
    });
}

private void OnRecipeLoadFailed(object? sender, string error)
{
    Dispatcher.Invoke(() =>
    {
        ComplianceContext.LogSystem(
            $"[Recipe] Load failed: {error}",
            LogLevel.Error, showInUi: true);
    });
}
```

---

## ?? 總結

### 優點
- ? 刪除了 ~100 行未使用的程式碼
- ? 提升可讀性和維護性
- ? 保留所有實際使用的功能
- ? 編譯成功，無任何錯誤

### 建議
1. **生產環境** - 保持當前清理後的版本
2. **開發環境** - 如果需要自動下載 Recipe，參考上述復原指南
3. **測試** - 使用手動測試按鈕取代自動測試

---

**清理完成時間：** 2024-12-29  
**清理前程式碼行數：** ~450 行  
**清理後程式碼行數：** ~350 行  
**減少比例：** ~22%
