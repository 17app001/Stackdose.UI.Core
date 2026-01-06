# ?? ThemeManager 主題管理系統 - 測試指南

## ?? 目錄
1. [快速開始](#快速開始)
2. [測試功能清單](#測試功能清單)
3. [測試步驟](#測試步驟)
4. [預期行為](#預期行為)
5. [進階測試](#進階測試)
6. [故障排除](#故障排除)

---

## ?? 快速開始

### **啟動應用程式**
1. 開啟 Visual Studio
2. 設定 `WpfApp1` 為啟動專案
3. 按下 `F5` 或點擊「啟動」按鈕

### **找到測試按鈕**
在主視窗的右側會看到 **「?? 主題測試」** 區塊，包含以下按鈕：

| 按鈕 | 功能 |
|------|------|
| ?? 切換 Dark/Light | 手動切換主題 |
| ?? 主題統計 | 顯示已註冊控制項統計 |
| ??? 開啟測試視窗 | 開啟完整功能展示視窗 |
| ?? 列印已註冊控制項 | 輸出到 Debug Console |
| ??? 清理失效參考 | 手動清理 WeakReference |

---

## ? 測試功能清單

### **1. 基本主題切換**
- [ ] Dark 主題顯示正常
- [ ] Light 主題顯示正常
- [ ] 主題切換即時生效
- [ ] 所有控制項同步更新

### **2. 控制項自動註冊**
- [ ] PlcLabel 自動註冊
- [ ] LiveLogViewer 自動註冊
- [ ] 自訂控制項可註冊

### **3. 記憶體管理**
- [ ] WeakReference 正常運作
- [ ] 失效參考可被清理
- [ ] 無記憶體洩漏

### **4. 統計與診斷**
- [ ] 統計資訊準確
- [ ] Debug Console 輸出正常
- [ ] 日誌記錄完整

---

## ?? 測試步驟

### **測試 1: 基本主題切換**

#### **步驟：**
1. 啟動應用程式（預設為 Dark 主題）
2. 觀察 PlcLabel、LiveLogViewer 等控制項顏色
3. 點擊 **「?? 切換 Dark/Light」** 按鈕
4. 觀察所有控制項是否同步變更顏色

#### **預期結果：**
```
? Dark 主題:
   - 背景: 深藍/深灰色 (#1E1E2E)
   - 文字: 淺色 (#BDBDBD)
   - 強調色: 霓虹藍 (#00E5FF)

? Light 主題:
   - 背景: 淺灰色 (#F5F5F5)
   - 文字: 深色 (#424242)
   - 強調色: 調整為適合淺色背景的顏色
```

#### **驗證點：**
- [ ] 主題切換即時生效（無需重啟）
- [ ] 所有 PlcLabel 底框顏色同步更新
- [ ] LiveLogViewer 日誌顏色同步更新
- [ ] CyberFrame 標題列顏色同步更新

---

### **測試 2: 主題統計資訊**

#### **步驟：**
1. 點擊 **「?? 主題統計」** 按鈕
2. 查看彈出的統計對話框

#### **預期結果：**
```
?? 主題管理統計資訊

當前主題: Dark (Dark)
變更時間: 2025-01-02 14:30:00

已註冊控制項:
  ? 總數: 12
  ? 存活: 12
  ? 失效: 0

記憶體效率: 100.0%
```

#### **驗證點：**
- [ ] 總數 > 0（代表有控制項註冊）
- [ ] 存活數 ? 總數（代表記憶體管理正常）
- [ ] 失效數應該很少（代表無洩漏）
- [ ] 記憶體效率 > 90%

---

### **測試 3: 完整功能展示視窗**

#### **步驟：**
1. 點擊 **「??? 開啟測試視窗」** 按鈕
2. 在新視窗中測試所有功能

#### **測試項目：**

**3.1 自訂控制項測試**
- 觀察 5 個 TestControl 是否正確顯示
- 每個 TestControl 顯示 "TestControl1: Waiting for theme..."

**3.2 主題切換測試**
- 點擊 **「切換到 Light 主題」**
- 觀察所有 TestControl 背景變為淺灰色
- 觀察文字更新為 "TestControl1: Light (Light) at 14:30:00"

**3.3 刷新主題測試**
- 點擊 **「刷新主題」**
- 觀察所有 TestControl 重新接收主題通知
- 統計資訊更新

**3.4 手動清理測試**
- 點擊 **「手動清理失效參考」**
- 查看清理結果對話框
- 統計資訊中「失效」數量應減少

**3.5 列印已註冊控制項**
- 點擊 **「列印已註冊控制項 (Debug)」**
- 開啟 Visual Studio 的「輸出」視窗
- 查看輸出內容

#### **預期 Debug Console 輸出：**
```
========== ThemeManager Registered Controls ==========
  [1] Stackdose.UI.Core.Controls.PlcLabel (Alive)
  [2] Stackdose.UI.Core.Controls.PlcLabel (Alive)
  [3] Stackdose.UI.Core.Controls.LiveLogViewer (Alive)
  [4] Stackdose.UI.Core.Examples.ThemeManagerDemoWindow+TestControl (Alive)
  [5] Stackdose.UI.Core.Examples.ThemeManagerDemoWindow+TestControl (Alive)
  ...
Total: 12, Alive: 12
====================================================
```

---

### **測試 4: 記憶體管理（WeakReference）**

#### **步驟：**
1. 點擊 **「??? 開啟測試視窗」** 按鈕（開啟多個測試視窗）
2. 點擊主視窗的 **「?? 主題統計」** 按鈕，記錄「總數」
3. 關閉所有測試視窗
4. 點擊 **「??? 清理失效參考」** 按鈕
5. 再次點擊 **「?? 主題統計」** 按鈕，比較「總數」

#### **預期結果：**
```
清理前: 20 個 (存活: 15, 失效: 5)
清理後: 15 個 (存活: 15, 失效: 0)

已移除 5 個失效參考
```

#### **驗證點：**
- [ ] 關閉視窗後，失效數量增加
- [ ] 清理後，失效數量歸零
- [ ] 總數 = 存活數（代表清理成功）
- [ ] 不影響正常運作的控制項

---

### **測試 5: 主題切換通知機制**

#### **步驟：**
1. 開啟 Visual Studio 的「輸出」視窗（Ctrl+Alt+O）
2. 點擊 **「?? 切換 Dark/Light」** 按鈕
3. 觀察 Debug Console 輸出

#### **預期 Debug Console 輸出：**
```
[CyberFrame] Applying Theme: Light
[CyberFrame] Removed: /Stackdose.UI.Core;component/Themes/Colors.xaml
[CyberFrame] Theme Applied Successfully: /Stackdose.UI.Core;component/Themes/LightColors.xaml
[ThemeManager] 主題已切換為 Light (Light)
[ThemeManager] 通知成功: 12, 失敗: 0
[PlcLabel] OnThemeChanged: Light (Light)
[LiveLogViewer] OnThemeChanged: Light (Light)
[CyberFrame] ThemeManager.SwitchTheme 已呼叫
```

#### **驗證點：**
- [ ] 所有已註冊控制項都收到通知
- [ ] 通知失敗數 = 0
- [ ] 主題資源字典正確載入/移除

---

## ?? 預期行為

### **? 正常行為**

1. **主題切換即時生效**
   - 無需重啟應用程式
   - 所有控制項同步更新
   - 動畫流暢（如果有）

2. **自動註冊/註銷**
   - 控制項 Loaded 時自動註冊
   - 控制項 Unloaded 時自動註銷
   - 無需手動管理

3. **記憶體安全**
   - 使用 WeakReference 儲存參考
   - GC 可正常回收控制項
   - 定期自動清理失效參考（30秒）

4. **執行緒安全**
   - 主題切換在 UI 執行緒執行
   - 批次通知使用鎖保護
   - 無競爭條件

### **? 異常行為（需修復）**

1. **主題切換無效**
   - 檢查資源字典路徑
   - 確認 ThemeManager 是否正確初始化

2. **控制項未更新**
   - 檢查是否實作 IThemeAware
   - 確認 Loaded/Unloaded 事件訂閱

3. **記憶體洩漏**
   - 檢查 WeakReference 使用
   - 確認清理機制運作正常

4. **統計資訊異常**
   - 重啟應用程式
   - 檢查 Debug Console 錯誤訊息

---

## ?? 進階測試

### **壓力測試：大量控制項**

#### **測試程式碼：**
```csharp
// 建立 100 個測試視窗
for (int i = 0; i < 100; i++)
{
    var demoWindow = new ThemeManagerDemoWindow();
    demoWindow.Show();
}

// 切換主題
ThemeManager.SwitchTheme(ThemeType.Light);

// 檢查統計
var stats = ThemeManager.GetStatistics();
// 預期: Total ? 600 (100 視窗 * 6 控制項)
```

#### **驗證點：**
- [ ] 所有控制項正常更新
- [ ] 無卡頓或延遲
- [ ] 記憶體使用量穩定

---

### **併發測試：多執行緒切換**

#### **測試程式碼：**
```csharp
// 同時從多個執行緒切換主題
Parallel.For(0, 10, i =>
{
    ThemeManager.SwitchTheme(i % 2 == 0 ? ThemeType.Dark : ThemeType.Light);
});

// 檢查最終狀態
var currentTheme = ThemeManager.CurrentTheme;
// 預期: 無例外，最終主題為最後一次切換的主題
```

#### **驗證點：**
- [ ] 無 ThreadingException
- [ ] 無資料競爭
- [ ] 最終狀態一致

---

## ?? 故障排除

### **問題 1: 主題切換後控制項未更新**

**可能原因：**
- 控制項未實作 IThemeAware
- 控制項未註冊到 ThemeManager
- Loaded 事件未觸發

**解決方法：**
1. 確認控制項實作 `IThemeAware` 介面
2. 確認在 `Loaded` 事件中呼叫 `ThemeManager.Register(this)`
3. 確認在 `Unloaded` 事件中呼叫 `ThemeManager.Unregister(this)`

**範例：**
```csharp
public partial class MyControl : UserControl, IThemeAware
{
    public MyControl()
    {
        InitializeComponent();
        Loaded += (s, e) => ThemeManager.Register(this);
        Unloaded += (s, e) => ThemeManager.Unregister(this);
    }

    public void OnThemeChanged(ThemeChangedEventArgs e)
    {
        // 更新控制項外觀
    }
}
```

---

### **問題 2: 記憶體洩漏**

**症狀：**
- 統計資訊中「失效」數量持續增加
- 記憶體使用量持續上升
- 應用程式變慢

**診斷步驟：**
1. 點擊 **「?? 主題統計」**，查看「失效」數量
2. 點擊 **「??? 清理失效參考」**
3. 再次查看統計，確認「失效」數量歸零

**如果問題持續：**
- 檢查是否有控制項未正確註銷
- 使用 Visual Studio 記憶體分析工具
- 查看 Debug Console 是否有錯誤訊息

---

### **問題 3: Debug Console 無輸出**

**檢查清單：**
- [ ] 確認是 Debug 組態（非 Release）
- [ ] 確認 Visual Studio 的「輸出」視窗已開啟（Ctrl+Alt+O）
- [ ] 確認「顯示輸出來源」選擇為「偵錯」
- [ ] 確認 #if DEBUG 區塊正常運作

---

### **問題 4: 主題統計資訊顯示為 0**

**可能原因：**
- 應用程式剛啟動，控制項尚未載入
- ThemeManager 未正確初始化

**解決方法：**
1. 等待視窗完全載入（約 1-2 秒）
2. 手動切換一次主題（觸發初始化）
3. 重啟應用程式

---

## ?? 效能基準

### **預期效能指標：**

| 指標 | 預期值 | 說明 |
|------|--------|------|
| 主題切換時間 | < 100ms | 從點擊到完全更新 |
| 控制項註冊時間 | < 1ms | 單一控制項註冊 |
| 批次通知時間 | < 50ms | 通知 100 個控制項 |
| 記憶體效率 | > 90% | 存活數 / 總數 |
| 清理時間 | < 10ms | 清理失效參考 |

### **效能測試程式碼：**
```csharp
using System.Diagnostics;

// 測試主題切換時間
var sw = Stopwatch.StartNew();
ThemeManager.SwitchTheme(ThemeType.Light);
sw.Stop();
Console.WriteLine($"Theme switch time: {sw.ElapsedMilliseconds}ms");

// 預期: < 100ms
```

---

## ?? 學習資源

### **相關檔案：**
- `ThemeManager.cs` - 主題管理器核心邏輯
- `IThemeAware.cs` - 主題感知介面定義
- `ThemeChangedEventArgs.cs` - 主題變更事件參數
- `ThemeManagerDemoWindow.cs` - 完整功能展示範例

### **關鍵技術：**
- WeakReference（弱引用）
- 觀察者模式（Observer Pattern）
- 依賴注入（Dependency Injection）
- WPF 資源字典（ResourceDictionary）

---

## ? 測試檢查清單

在完成測試後，請確認以下項目：

- [ ] 主題切換功能正常
- [ ] 所有控制項同步更新
- [ ] 統計資訊準確
- [ ] Debug Console 輸出正常
- [ ] 無記憶體洩漏
- [ ] 無效能問題
- [ ] 無例外錯誤
- [ ] 日誌記錄完整

---

## ?? 測試報告範本

```markdown
# ThemeManager 測試報告

**測試日期：** 2025-01-02  
**測試人員：** [您的名字]  
**應用程式版本：** 1.0.0  

## 測試結果

| 測試項目 | 結果 | 備註 |
|---------|------|------|
| 基本主題切換 | ? 通過 | 所有控制項正常更新 |
| 主題統計資訊 | ? 通過 | 統計準確 |
| 完整功能展示視窗 | ? 通過 | 所有功能正常 |
| 記憶體管理 | ? 通過 | 無洩漏，清理正常 |
| 主題切換通知機制 | ? 通過 | 所有控制項收到通知 |

## 效能數據

- 主題切換時間: 85ms
- 控制項註冊數量: 15
- 記憶體效率: 100%

## 問題與建議

（無）

## 總結

ThemeManager 主題管理系統運作正常，符合預期。
```

---

## ?? 完成！

恭喜您完成 ThemeManager 的測試！如果所有測試都通過，代表：

? **主題管理系統完全正常**  
? **記憶體管理機制運作良好**  
? **統一通知機制正確實作**  
? **可以正式投入生產環境使用**

如有任何問題，請查看 [故障排除](#故障排除) 章節或聯繫開發團隊。
