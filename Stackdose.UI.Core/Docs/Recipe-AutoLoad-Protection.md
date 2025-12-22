# Recipe Auto-Load 防重複載入機制

## 問題描述

Recipe 自動載入時可能發生無限迴圈或重複載入,導致應用程式當機。

---

## 防護機制

### 1. **RecipeLoader 實例層級防護** (`_isAutoLoading`)

```csharp
private bool _isAutoLoading = false; // 每個 RecipeLoader 實例的旗標

private void OnPlcConnectionEstablished(IPlcManager plcManager)
{
    if (_isAutoLoading) return; // 防止同一實例重複載入
    _isAutoLoading = true;
    
    try {
        await AutoLoadRecipeAsync();
    } finally {
        _isAutoLoading = false;
    }
}
```

**用途**: 防止同一個 `RecipeLoader` 實例因事件重複觸發而多次載入

---

### 2. **RecipeContext 靜態層級防護** (`_isAutoLoadInProgress`)

```csharp
private static bool _isAutoLoadInProgress = false; // 全域靜態旗標

public static async Task<bool> LoadRecipeAsync(string filePath, bool isAutoLoad = false)
{
    if (isAutoLoad)
    {
        if (_isAutoLoadInProgress) return false; // 防止多實例同時載入
        _isAutoLoadInProgress = true;
    }
    
    try {
        // 載入 Recipe...
    } finally {
        if (isAutoLoad) {
            _isAutoLoadInProgress = false;
        }
    }
}
```

**用途**: 防止多個 `RecipeLoader` 實例同時進行自動載入

---

### 3. **IsInitialized 狀態檢查**

```csharp
if (RecipeContext.IsInitialized) return; // 已初始化,不再載入
```

**用途**: 確保 Recipe 只會被載入一次

---

## 防護流程

```
[PLC Connected]
    ↓
[ConnectionEstablished Event Triggered]
    ↓
┌─────────────────────────────────────┐
│ RecipeLoader Instance Check         │
│ - _isAutoLoading? → Skip            │
│ - RecipeContext.IsInitialized? → Skip │
│ - !AutoLoadOnStartup? → Skip        │
└─────────────────────────────────────┘
    ↓ Pass all checks
[Set _isAutoLoading = true]
    ↓
┌─────────────────────────────────────┐
│ RecipeContext Static Check          │
│ - _isAutoLoadInProgress? → Skip     │
└─────────────────────────────────────┘
    ↓ Pass all checks
[Set _isAutoLoadInProgress = true]
    ↓
[Load Recipe]
    ↓
[Set IsInitialized = true]
    ↓
[Finally: Reset both flags]
```

---

## 日誌追蹤

啟用詳細日誌來診斷問題:

```csharp
ComplianceContext.LogSystem(
    $"[RecipeLoader] Event received. _isAutoLoading={_isAutoLoading}, IsInitialized={RecipeContext.IsInitialized}",
    LogLevel.Info,
    showInUi: false // 不顯示在 UI,避免干擾
);
```

### 正常流程日誌

```
10:00:01.0  [RecipeLoader] PLC connection established event received. _isAutoLoading=false, IsInitialized=false, AutoLoadOnStartup=true
10:00:01.1  [RecipeLoader] Starting auto-load...
10:00:01.2  [Recipe] Auto-load in progress check: _isAutoLoadInProgress=false
10:00:01.3  [Recipe] Loading Recipe.json...
10:00:01.5  [Recipe] Successfully loaded Recipe: Standard Process Recipe A v1.2.0 (14 parameters)
10:00:01.6  [Recipe] Registered 14/14 parameters to PLC Monitor
```

### 防止重複載入日誌

```
10:00:01.0  [RecipeLoader] Event received. _isAutoLoading=false, IsInitialized=false
10:00:01.1  [RecipeLoader] Starting auto-load...
10:00:01.2  [Recipe] Loading...
10:00:01.3  [RecipeLoader] Event received again. _isAutoLoading=true ← 第一層防護
10:00:01.3  [RecipeLoader] Skip: Already loading
10:00:01.5  [Recipe] Load completed
10:00:01.6  [RecipeLoader] Event received again. IsInitialized=true ← 第二層防護
10:00:01.6  [RecipeLoader] Skip: Already initialized
```

---

## 可能的問題場景

### 場景 1: 多個 RecipeLoader 實例

```xml
<!-- ? 錯誤: 多個 RecipeLoader -->
<Controls:RecipeLoader AutoLoadOnStartup="True" />
<Controls:RecipeLoader AutoLoadOnStartup="True" />
```

**防護**: `_isAutoLoadInProgress` 靜態旗標確保只有第一個實例能載入

---

### 場景 2: ConnectionEstablished 重複觸發

可能原因:
- PLC 重連
- Monitor 掃描更新
- 事件訂閱沒有正確移除

**防護**: 
- `_isAutoLoading` 實例旗標
- `RecipeContext.IsInitialized` 狀態檢查

---

### 場景 3: Dispatcher 死鎖

```csharp
// ? 可能死鎖
await Dispatcher.InvokeAsync(async () => await LoadAsync());

// ? 正確做法
Dispatcher.BeginInvoke(new Action(async () => await LoadAsync()));
```

---

## 測試檢查清單

### ? 正常載入測試

1. 啟動應用程式
2. PLC 連線成功
3. Recipe 自動載入一次
4. 日誌顯示成功訊息
5. UI 顯示 Recipe 資訊

### ? 防重複測試

1. 啟動應用程式
2. PLC 連線成功
3. Recipe 開始載入
4. 模擬第二次觸發 (如果可能)
5. 檢查日誌: 應顯示 "Skip: Already loading"
6. 確認只載入一次

### ? 重連測試

1. Recipe 已載入
2. PLC 斷線
3. PLC 重新連線
4. 檢查日誌: 應顯示 "Skip: Already initialized"
5. Recipe 不會重新載入

---

## 疑難排解

### 問題: 無限載入

**檢查**:
1. 日誌中是否有重複的 "Event received" 訊息?
2. `_isAutoLoading` 旗標是否正確重置?
3. 是否有多個 RecipeLoader 實例?

**解決**:
```csharp
// 確保 finally 區塊執行
finally {
    _isAutoLoading = false;
    if (isAutoLoad) _isAutoLoadInProgress = false;
}
```

---

### 問題: 不會自動載入

**檢查**:
1. `AutoLoadOnStartup="True"` 是否設定?
2. PLC 是否已連線?
3. `RequirePlcConnection="True"` 是否設定?
4. `RecipeContext.IsInitialized` 是否為 false?

**解決**:
```xml
<Controls:RecipeLoader 
    AutoLoadOnStartup="True"
    RequirePlcConnection="True"/>
```

---

### 問題: 載入後當機

**檢查**:
1. Recipe.json 格式是否正確?
2. Monitor 註冊是否失敗?
3. 是否有異常未被捕捉?

**解決**:
```csharp
try {
    await LoadRecipeAsync();
} catch (Exception ex) {
    ComplianceContext.LogSystem($"Exception: {ex}", LogLevel.Error);
}
```

---

## 最佳實踐

### 1. 只使用一個 RecipeLoader

```xml
<!-- ? 推薦 -->
<Controls:RecipeLoader AutoLoadOnStartup="True"/>

<!-- ? 避免 -->
<Controls:RecipeLoader AutoLoadOnStartup="True"/>
<Controls:RecipeLoader AutoLoadOnStartup="True"/>
```

### 2. 正確取消訂閱事件

```csharp
this.Unloaded += (s, e) => {
    var plcStatus = PlcContext.GlobalStatus;
    if (plcStatus != null) {
        plcStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
    }
};
```

### 3. 使用 BeginInvoke 避免死鎖

```csharp
Dispatcher.BeginInvoke(new Action(async () => {
    await AutoLoadRecipeAsync();
}));
```

---

## 總結

三層防護機制:
1. ? **實例層級** - `_isAutoLoading` (防止單一實例重複)
2. ? **靜態層級** - `_isAutoLoadInProgress` (防止多實例衝突)
3. ? **狀態檢查** - `IsInitialized` (防止重複初始化)

確保 Recipe 只會在 PLC 第一次連線成功後載入一次!
