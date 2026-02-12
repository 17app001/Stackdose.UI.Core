# Control Base Abstraction - Migration Guide

## ?? 目標

將現有控件遷移到使用統一的基類，減少重複代碼，提升維護性。

---

## ??? 新增的基類架構

### 1. **CyberControlBase** (通用控件基類)

提供所有控件的基礎功能：

- ? 自動設計時檢測
- ? 統一的 Loaded/Unloaded 生命週期管理
- ? 主題感知自動註冊/註銷
- ? 線程安全的 Dispatcher 操作
- ? 資源清理管理 (IDisposable)

### 2. **PlcControlBase** (PLC 控件基類)

繼承自 `CyberControlBase`，額外提供 PLC 相關功能：

- ? 自動綁定到全域或指定的 PlcStatus
- ? 自動訂閱/取消訂閱 PLC 事件
- ? 統一的連線狀態處理
- ? 執行緒安全的 PLC 操作

---

## ?? 遷移步驟

### **範例：將 PlcText 遷移到 PlcControlBase**

#### **遷移前（舊代碼）**

```csharp
public partial class PlcText : UserControl
{
    private PlcStatus? _subscribedStatus;
    
    public PlcText()
    {
        InitializeComponent();
        Loaded += PlcText_Loaded;
        Unloaded += PlcText_Unloaded;
    }
    
    private void PlcText_Loaded(object sender, RoutedEventArgs e)
    {
        SubscribeToGlobalStatus();
        if (!string.IsNullOrEmpty(Address))
        {
            ReadFromPlc();
        }
    }
    
    private void PlcText_Unloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromStatus();
    }
    
    private void SubscribeToGlobalStatus()
    {
        UnsubscribeFromStatus();
        var globalStatus = PlcContext.GlobalStatus;
        if (globalStatus != null)
        {
            _subscribedStatus = globalStatus;
            _subscribedStatus.ConnectionEstablished += OnPlcConnectionEstablished;
            _subscribedStatus.ScanUpdated += OnScanUpdated;
        }
    }
    
    private void UnsubscribeFromStatus()
    {
        if (_subscribedStatus != null)
        {
            _subscribedStatus.ConnectionEstablished -= OnPlcConnectionEstablished;
            _subscribedStatus.ScanUpdated -= OnScanUpdated;
            _subscribedStatus = null;
        }
    }
    
    private void OnPlcConnectionEstablished(IPlcManager manager)
    {
        try
        {
            if (Dispatcher.HasShutdownStarted) return;
            Dispatcher.Invoke(() => {
                if (!Dispatcher.HasShutdownStarted && !string.IsNullOrEmpty(Address))
                {
                    ReadFromPlc();
                }
            });
        }
        catch (Exception ex)
        {
            // Error handling
        }
    }
    
    private void OnScanUpdated(IPlcManager manager)
    {
        // Update logic
    }
}
```

#### **遷移後（新代碼）**

```csharp
public partial class PlcText : PlcControlBase
{
    public PlcText()
    {
        InitializeComponent();
    }
    
    // ? 覆寫：PLC 連線建立時
    protected override void OnPlcConnected(IPlcManager manager)
    {
        base.OnPlcConnected(manager);
        
        if (!string.IsNullOrEmpty(Address))
        {
            ReadFromPlc();
        }
    }
    
    // ? 覆寫：PLC 數據更新時
    protected override void OnPlcDataUpdated(IPlcManager manager)
    {
        base.OnPlcDataUpdated(manager);
        RefreshFrom(manager);
    }
    
    // ? 使用基類提供的 SafeInvoke（不再需要手動檢查 Dispatcher）
    private void ReadFromPlc()
    {
        SafeInvoke(() =>
        {
            var manager = GetPlcManager();
            if (manager == null || !manager.IsConnected || string.IsNullOrEmpty(Address))
            {
                return;
            }
            
            short? readValue = manager.ReadWord(Address);
            if (readValue.HasValue)
            {
                Value = readValue.Value.ToString();
            }
        });
    }
}
```

#### **減少的代碼量**

| 項目 | 舊代碼行數 | 新代碼行數 | 減少 |
|------|-----------|-----------|------|
| 事件訂閱/取消訂閱 | ~30 行 | 0 行 | -30 行 |
| Dispatcher 檢查 | ~10 行 | 0 行 | -10 行 |
| Loaded/Unloaded | ~15 行 | 0 行 | -15 行 |
| **總計** | **~55 行** | **~15 行** | **-73% ??** |

---

## ?? 遷移檢查清單

針對每個控件，按照以下步驟進行遷移：

### **步驟 1: 確認控件類型**

- [ ] 與 PLC 相關？ → 繼承 `PlcControlBase`
- [ ] 與 PLC 無關？ → 繼承 `CyberControlBase`

### **步驟 2: 移除舊代碼**

- [ ] 移除 `Loaded/Unloaded` 事件訂閱
- [ ] 移除 `_subscribedStatus` 等私有欄位
- [ ] 移除 `SubscribeToXXX` / `UnsubscribeFromXXX` 方法
- [ ] 移除手動的 `Dispatcher.Invoke` 檢查

### **步驟 3: 覆寫新方法**

- [ ] 實作 `OnPlcConnected()` （PLC 連線成功時）
- [ ] 實作 `OnPlcDataUpdated()` （PLC 數據更新時）
- [ ] 實作 `OnThemeChanged()` （主題變更時，如需要）

### **步驟 4: 使用基類 Helper**

- [ ] 使用 `SafeInvoke()` / `SafeBeginInvoke()` 取代手動 Dispatcher 檢查
- [ ] 使用 `GetPlcManager()` 取得 PLC Manager
- [ ] 使用 `IsPlcConnected()` 檢查連線狀態

### **步驟 5: 測試**

- [ ] 在設計器中打開 XAML，確認不會崩潰
- [ ] 執行程式，確認功能正常
- [ ] 切換主題，確認主題變更正常
- [ ] 多次切換頁面，確認沒有記憶體洩漏

---

## ?? 遷移優先順序

### **Phase 1: PLC 相關控件（高優先）**

1. ? `PlcText` - 示範控件（已完成）
2. ? `PlcLabel` - 最常用
3. ? `PlcStatus` - 核心控件
4. ? `PrintHeadStatus` - 自訂控件
5. ? `PrintHeadController` - 自訂控件

### **Phase 2: UI 控件（中優先）**

1. ? `CyberFrame` - 主框架
2. ? `LiveLogViewer` - 日誌檢視器
3. ? `AlarmViewer` - 警報檢視器
4. ? `SensorViewer` - 感測器檢視器

### **Phase 3: 對話框控件（低優先）**

1. ? `LoginDialog`
2. ? `UserEditorDialog`
3. ? `InputDialog`
4. ? `BatchInputDialog`

---

## ?? 最佳實踐

### **1. 不要過度抽象**

? **錯誤**：為每個控件都創建一個專屬基類

```csharp
// 不推薦 - 過度細分
public class PlcTextBase : PlcControlBase { }
public class PlcLabelBase : PlcControlBase { }
```

? **正確**：只在有多個控件共享相同邏輯時才創建基類

```csharp
// PlcText 和 PlcLabel 都直接繼承 PlcControlBase
public class PlcText : PlcControlBase { }
public class PlcLabel : PlcControlBase { }
```

### **2. 使用組合優於繼承（當適用時）**

如果某些功能只有少數控件需要，考慮使用 Helper 類別而非繼承：

```csharp
// 例如：數值格式化功能
public static class PlcValueFormatter
{
    public static string Format(double value, double divisor, string format)
    {
        return (value / divisor).ToString(format);
    }
}

// 在控件中使用
Value = PlcValueFormatter.Format(readValue, Divisor, StringFormat);
```

### **3. 保持向後兼容**

遷移時，不要立即刪除舊代碼，而是：

1. 創建新的基類版本（例如 `PlcTextV2`）
2. 測試新版本
3. 確認無問題後，再替換舊版本

### **4. 文件化變更**

在每個遷移的控件頂部添加註釋：

```csharp
/// <summary>
/// PlcText - 可編輯的 PLC 參數控件
/// </summary>
/// <remarks>
/// ? 已遷移至 PlcControlBase (2024-XX-XX)
/// - 移除手動事件訂閱邏輯
/// - 使用基類的 SafeInvoke
/// - 減少 ~50 行重複代碼
/// </remarks>
public partial class PlcText : PlcControlBase
{
    // ...
}
```

---

## ?? 預期效益

### **代碼量減少**

| 控件 | 遷移前 | 遷移後 | 減少比例 |
|------|--------|--------|---------|
| PlcText | ~200 行 | ~120 行 | -40% |
| PlcLabel | ~250 行 | ~150 行 | -40% |
| PlcStatus | ~300 行 | ~200 行 | -33% |
| **總計** | **~750 行** | **~470 行** | **-37% ??** |

### **維護性提升**

- ? Bug 修復只需改一個地方（基類）
- ? 新增功能自動套用到所有控件
- ? 更容易 onboarding 新成員

### **效能提升**

- ? 減少重複的事件訂閱邏輯
- ? 統一的記憶體清理機制
- ? 更少的記憶體洩漏風險

---

## ? FAQ

### **Q: 是否所有控件都需要遷移？**

**A:** 不是。優先遷移：
- 有重複代碼的控件（PLC 相關）
- 維護頻繁的控件
- 容易出 Bug 的控件

### **Q: 遷移會破壞現有功能嗎？**

**A:** 不會，基類只是將現有邏輯抽象化，不改變行為。

### **Q: 如果遇到問題怎麼辦？**

**A:** 
1. 先檢查是否覆寫了正確的方法
2. 檢查 Debug 輸出，基類會輸出詳細的生命週期資訊
3. 如果無法解決，回退到舊版本

---

## ?? 聯絡資訊

遷移過程中遇到問題？請聯絡：
- GitHub Issues: [專案 Issue 頁面]
- Email: [維護者 Email]

---

**最後更新**: 2024-XX-XX
**版本**: 1.0.0
