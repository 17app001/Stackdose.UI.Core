# Control Base - Quick Reference Card

## ?? 快速開始

### **1. 選擇正確的基類**

```csharp
// PLC 相關控件 → PlcControlBase
public partial class MyPlcControl : PlcControlBase { }

// 一般 UI 控件 → CyberControlBase
public partial class MyUiControl : CyberControlBase { }
```

---

## ?? CyberControlBase - API 速查

### **生命週期鉤子**

```csharp
protected override void OnControlLoaded()
{
    // 控件載入時的邏輯
}

protected override void OnControlUnloaded()
{
    // 控件卸載時的邏輯
}

protected override void OnThemeChanged(ThemeChangedEventArgs e)
{
    // 主題變更時的邏輯
    bool isLight = e.IsLightTheme;
    string themeName = e.ThemeName;
}
```

### **Helper 方法**

```csharp
// UI 執行緒安全操作（同步）
SafeInvoke(() =>
{
    // 你的 UI 操作
});

// UI 執行緒安全操作（非同步）
SafeBeginInvoke(() =>
{
    // 你的 UI 操作
});
```

### **屬性**

```csharp
bool isLoaded = IsControlLoaded;      // 控件是否已載入
bool isDesignMode = IsInDesignMode;   // 是否在設計模式
bool disposed = IsDisposed;            // 控件是否已釋放
```

---

## ?? PlcControlBase - API 速查

### **生命週期鉤子**

```csharp
protected override void OnPlcConnected(IPlcManager manager)
{
    // PLC 連線成功時的邏輯
    base.OnPlcConnected(manager);
    
    // 讀取初始值
    ReadFromPlc();
}

protected override void OnPlcDataUpdated(IPlcManager manager)
{
    // PLC 數據更新時的邏輯
    base.OnPlcDataUpdated(manager);
    
    // 刷新顯示
    RefreshData(manager);
}
```

### **Helper 方法**

```csharp
// 取得 PlcManager
var manager = GetPlcManager();
if (manager != null && manager.IsConnected)
{
    // 讀寫 PLC
}

// 檢查連線狀態
if (IsPlcConnected())
{
    // PLC 相關操作
}
```

### **Dependency Properties**

```csharp
// 指定 PLC Manager
<MyControl PlcManager="{Binding MyPlcManager}" />

// 綁定到特定 PlcStatus
<MyControl TargetStatus="{Binding ElementName=MyPlcStatus}" />
```

---

## ? 常用模式

### **模式 1: 讀取 PLC 數據**

```csharp
private void ReadFromPlc()
{
    SafeInvoke(() =>
    {
        var manager = GetPlcManager();
        if (manager == null || !IsPlcConnected())
        {
            return;
        }
        
        short? value = manager.ReadWord("D100");
        if (value.HasValue)
        {
            MyValue = value.Value.ToString();
        }
    });
}
```

### **模式 2: 寫入 PLC 數據**

```csharp
private async Task WriteToPlc()
{
    var manager = GetPlcManager();
    if (manager == null || !IsPlcConnected())
    {
        ShowError("PLC not connected");
        return;
    }
    
    bool success = await manager.WriteAsync("D100,123");
    if (success)
    {
        ShowSuccess("Write successful");
    }
}
```

### **模式 3: 處理主題變更**

```csharp
protected override void OnThemeChanged(ThemeChangedEventArgs e)
{
    base.OnThemeChanged(e);
    
    // 更新顏色
    if (e.IsLightTheme)
    {
        MyBorder.Background = Brushes.White;
    }
    else
    {
        MyBorder.Background = Brushes.Black;
    }
}
```

---

## ?? 遷移檢查清單

### **步驟 1: 更改繼承**

```csharp
// Before
public partial class MyControl : UserControl

// After (PLC 相關)
public partial class MyControl : PlcControlBase

// After (一般 UI)
public partial class MyControl : CyberControlBase
```

### **步驟 2: 移除舊代碼**

```csharp
// ? 移除
this.Loaded += MyControl_Loaded;
this.Unloaded += MyControl_Unloaded;

// ? 移除
private PlcStatus? _subscribedStatus;
private void SubscribeToGlobalStatus() { ... }
private void UnsubscribeFromStatus() { ... }

// ? 移除
if (Dispatcher.HasShutdownStarted) return;
Dispatcher.Invoke(() => { ... });
```

### **步驟 3: 覆寫新方法**

```csharp
// ? 新增
protected override void OnControlLoaded()
{
    // 原本 Loaded 的邏輯
}

// ? 新增（PLC 控件）
protected override void OnPlcConnected(IPlcManager manager)
{
    // 原本 ConnectionEstablished 的邏輯
}

protected override void OnPlcDataUpdated(IPlcManager manager)
{
    // 原本 ScanUpdated 的邏輯
}
```

### **步驟 4: 使用基類 Helper**

```csharp
// ? Before
if (!Dispatcher.HasShutdownStarted)
{
    Dispatcher.Invoke(() =>
    {
        // UI 操作
    });
}

// ? After
SafeInvoke(() =>
{
    // UI 操作
});

// ? Before
var manager = PlcManager ?? PlcContext.GlobalStatus?.CurrentManager;

// ? After
var manager = GetPlcManager();
```

---

## ?? 常見問題

### **Q: Loaded 事件沒有觸發？**

```csharp
// ? 錯誤：忘記呼叫 base
protected override void OnControlLoaded()
{
    // 你的邏輯
}

// ? 正確：一定要呼叫 base
protected override void OnControlLoaded()
{
    base.OnControlLoaded();  // ← 必須
    // 你的邏輯
}
```

### **Q: PLC 連線後無法讀取數據？**

```csharp
// 檢查 1: 確認有覆寫 OnPlcConnected
protected override void OnPlcConnected(IPlcManager manager)
{
    base.OnPlcConnected(manager);
    ReadFromPlc();  // 連線後讀取
}

// 檢查 2: 確認有覆寫 OnPlcDataUpdated
protected override void OnPlcDataUpdated(IPlcManager manager)
{
    base.OnPlcDataUpdated(manager);
    RefreshData(manager);  // 數據更新時刷新
}
```

### **Q: 主題變更沒有反應？**

```csharp
// 確認有實作 OnThemeChanged
protected override void OnThemeChanged(ThemeChangedEventArgs e)
{
    base.OnThemeChanged(e);
    UpdateColors(e.IsLightTheme);
}
```

---

## ?? 效能對比

| 操作 | 舊方法 | 新方法 | 改善 |
|------|--------|--------|------|
| 事件訂閱 | 每次 Loaded 手動訂閱 | 基類自動處理 | ? |
| Dispatcher 檢查 | 每次手動檢查 | SafeInvoke | ? |
| 記憶體清理 | 容易遺漏 | IDisposable 自動清理 | ? |
| 代碼行數 | ~200 行 | ~120 行 | -40% |

---

## ?? 最佳實踐

### **? DO**

```csharp
// 一定要呼叫 base
protected override void OnControlLoaded()
{
    base.OnControlLoaded();
}

// 使用 SafeInvoke
SafeInvoke(() => { /* UI */ });

// 使用 GetPlcManager()
var mgr = GetPlcManager();
```

### **? DON'T**

```csharp
// 不要手動訂閱 Loaded/Unloaded
this.Loaded += MyControl_Loaded;  // ?

// 不要手動 Dispatcher.Invoke
Dispatcher.Invoke(() => { });     // ?

// 不要手動管理 PLC 訂閱
_plcStatus.ScanUpdated += ...;   // ?
```

---

## ?? 相關文件

- ?? [完整遷移指南](MIGRATION_GUIDE.md)
- ?? [API 參考文件](API_REFERENCE.md)
- ?? [Batch C 總結](BATCH_C_CONTROLS_BASE_SUMMARY.md)

---

**最後更新**: 2024-XX-XX  
**版本**: 1.0.0
