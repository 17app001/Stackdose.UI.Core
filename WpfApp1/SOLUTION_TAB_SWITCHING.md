# ?? 解決 Tab 切換時 PLC 連線重置問題

## ?? 問題描述

**現象：**
- 切換 Tab 回到首頁時，PLC 會重新連線
- 切換到其他 Tab 時，顯示「沒有連線」
- `PlcLabel` 等控件無法讀取 PLC 數據

**根本原因：**
1. `PlcStatus` 原本放在 `MainPanel.xaml` 中
2. 每次切換 Tab 時，WPF 會卸載（Unload）和重新載入（Load）`MainPanel`
3. `PlcStatus` 的 `Dispose()` 方法會被呼叫，導致 PLC 連線被關閉
4. 重新載入時會再次執行 `AutoConnect`，造成重複連線

---

## ? 解決方案

### ?? 核心策略：將 `PlcStatus` 提升到 `MainWindow` 層級

```
MainWindow
  └─ PlcStatus (IsGlobal="True", Visibility="Collapsed")  ← 全域且隱藏
  └─ TabControl
      ├─ MainPanel  ← 不再包含 PlcStatus
      └─ SystemTestPanel
```

### ?? 修改內容

#### 1. **MainWindow.xaml** - 新增全域 PlcStatus

```xaml
<Controls:CyberFrame Title="UBI SYSTEM">
    <Controls:CyberFrame.MainContent>
        <Grid>
            <!-- ?? Global PlcStatus (隱藏但保持連線) -->
            <Controls:PlcStatus 
                IpAddress="192.168.22.39" 
                Port="3000" 
                AutoConnect="True" 
                IsGlobal="True"
                MonitorAddress="" 
                MonitorLength="1" 
                ScanInterval="120"
                Visibility="Collapsed"/>
            
            <!-- TabControl for organized layout -->
            <TabControl>
                ...
            </TabControl>
        </Grid>
    </Controls:CyberFrame.MainContent>
</Controls:CyberFrame>
```

**關鍵屬性：**
- ? `IsGlobal="True"` → 註冊到 `PlcContext.GlobalStatus`
- ? `Visibility="Collapsed"` → 隱藏 UI，但保持功能運作
- ? `AutoConnect="True"` → 應用程式啟動時自動連線

#### 2. **MainPanel.xaml** - 移除 PlcStatus

```xaml
<!-- Before -->
<Controls:PlcStatus Grid.Row="1"
    IpAddress="192.168.22.39" 
    Port="3000" 
    AutoConnect="True" 
    IsGlobal="True"
    .../>

<!-- After -->
<!-- PlcStatus 已移至 MainWindow，此處不再需要 -->
```

---

## ?? 效果

### ? **問題已解決**

| 修改前 | 修改後 |
|--------|--------|
| ? 切換 Tab 會重新連線 PLC | ? PLC 連線保持不中斷 |
| ? 其他 Tab 無法讀取 PLC 數據 | ? 所有 Tab 共用同一個 PLC 連線 |
| ? 每次切換都會看到 "CONNECTING..." | ? 始終保持 "CONNECTED" 狀態 |
| ? PlcLabel 顯示 "-" | ? 正確顯示 PLC 數據 |

---

## ?? 工作原理

### 1. **PlcContext.GlobalStatus**

```csharp
// PlcStatus.cs (OnLoaded)
if (IsGlobal)
{
    PlcContext.GlobalStatus = this;
}
```

當 `IsGlobal="True"` 時，`PlcStatus` 會將自己註冊到全域靜態屬性 `PlcContext.GlobalStatus`。

### 2. **PlcLabel 自動綁定**

```csharp
// PlcLabel.cs (OnLoaded)
private void TryResolveContextStatus()
{
    var contextStatus = PlcContext.GetStatus(this) ?? PlcContext.GlobalStatus;
    if (contextStatus != null) BindToStatus(contextStatus);
}
```

所有 `PlcLabel` 控件會自動尋找並綁定到 `PlcContext.GlobalStatus`。

### 3. **生命週期管理**

```
MainWindow Loaded
  └─ PlcStatus Loaded → 連線 PLC → 註冊到 GlobalStatus
  
Tab 切換
  └─ MainPanel Unloaded/Loaded → 不影響 PlcStatus
  
MainWindow Closed
  └─ PlcStatus Disposed → 關閉 PLC 連線
```

---

## ?? 架構圖

### Before (有問題)
```
MainWindow
  └─ TabControl
      └─ MainPanel
          └─ PlcStatus (IsGlobal)  ← 每次切換 Tab 都會被銷毀/重建
```

### After (正確)
```
MainWindow
  ├─ PlcStatus (IsGlobal, Hidden)  ← 永久存在，不受 Tab 切換影響
  └─ TabControl
      ├─ MainPanel
      │   ├─ PlcLabel (自動綁定到 GlobalStatus)
      │   └─ SensorViewer (自動綁定到 GlobalStatus)
      └─ SystemTestPanel
          └─ PlcLabel (自動綁定到 GlobalStatus)
```

---

## ?? 最佳實踐

### ? **推薦做法**

1. **全域 PLC 連線** → 放在 `MainWindow` 層級
2. **隱藏 UI** → `Visibility="Collapsed"`（不顯示但保持運作）
3. **使用 IsGlobal** → 讓所有子控件自動找到連線
4. **避免重複連線** → 一個應用程式只需要一個 `PlcStatus`

### ? **避免做法**

1. ? 在 `UserControl` (Tab 內容) 中放置 `PlcStatus`
2. ? 在多個 Tab 中重複建立 `PlcStatus`
3. ? 手動管理 PLC 連線的生命週期

---

## ?? 故障排除

### 問題 1：切換 Tab 後 PlcLabel 仍顯示 "-"

**原因：** `PlcStatus` 沒有設定 `IsGlobal="True"`

**解決：**
```xaml
<Controls:PlcStatus IsGlobal="True" ... />
```

### 問題 2：應用程式啟動時沒有自動連線

**原因：** `PlcStatus` 沒有設定 `AutoConnect="True"`

**解決：**
```xaml
<Controls:PlcStatus AutoConnect="True" ... />
```

### 問題 3：MainWindow 中看不到 PLC 狀態指示燈

**原因：** `Visibility="Collapsed"` 隱藏了 UI

**解決方案 A（推薦）：** 保持隱藏，在狀態列或其他位置顯示狀態
```csharp
// MainWindow.xaml.cs
PlcContext.GlobalStatus.ScanUpdated += (manager) => 
{
    StatusTextBlock.Text = "PLC Online";
};
```

**解決方案 B：** 顯示 PlcStatus UI
```xaml
<Controls:PlcStatus Visibility="Visible" ... />
```

---

## ?? 更新日期

**版本：** 1.0  
**日期：** 2025-01-06  
**作者：** GitHub Copilot  

---

## ?? 相關檔案

- `WpfApp1/MainWindow.xaml` - PlcStatus 新位置
- `WpfApp1/Panels/MainPanel.xaml` - 移除了 PlcStatus
- `Stackdose.UI.Core/Controls/PlcStatus.xaml.cs` - PLC 連線邏輯
- `Stackdose.UI.Core/Helpers/PlcContext.cs` - 全域連線管理
- `Stackdose.UI.Core/Controls/PlcLabel.xaml.cs` - 自動綁定邏輯

---

## ? 驗證清單

執行應用程式並確認：

- [ ] 首頁載入時 PLC 自動連線
- [ ] `PlcLabel` 顯示正確的 PLC 數據
- [ ] 切換到 "System Test" Tab
- [ ] `PlcLabel` 仍顯示正確的數據（不是 "-"）
- [ ] 切換回 "Main" Tab
- [ ] PLC 沒有重新連線（檢查日誌）
- [ ] 所有 `PlcLabel` 持續更新數據
- [ ] 關閉應用程式時 PLC 正確斷線

---

## ?? 學習要點

1. **WPF 控件生命週期** → `Loaded` / `Unloaded` 事件
2. **TabControl 行為** → 切換 Tab 會卸載非選中的內容
3. **全域狀態管理** → 使用靜態屬性共享資源
4. **依賴注入模式** → `PlcContext.GlobalStatus` 作為服務定位器
5. **UI 與邏輯分離** → `Visibility="Collapsed"` 保留功能但隱藏 UI

---

## ?? 進階優化（可選）

### 1. **顯示全域 PLC 狀態**

在 `MainWindow` 的狀態列顯示 PLC 狀態：

```xaml
<StatusBar DockPanel.Dock="Bottom">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <Ellipse x:Name="PlcStatusLight" Width="10" Height="10" Fill="Red"/>
            <TextBlock x:Name="PlcStatusText" Text="PLC Disconnected" Margin="5,0"/>
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

```csharp
// MainWindow.xaml.cs
private void MainWindow_Loaded(object sender, RoutedEventArgs e)
{
    if (PlcContext.GlobalStatus != null)
    {
        PlcContext.GlobalStatus.ScanUpdated += (manager) =>
        {
            Dispatcher.Invoke(() =>
            {
                PlcStatusLight.Fill = new SolidColorBrush(Colors.LimeGreen);
                PlcStatusText.Text = "PLC Connected";
            });
        };
    }
}
```

### 2. **監控連線狀態變化**

```csharp
// 訂閱 PlcStatus 的 ConnectionStateChanged 事件
PlcContext.GlobalStatus.ConnectionStateChanged += (state) =>
{
    ComplianceContext.LogSystem($"PLC State: {state}", LogLevel.Info);
};
```

---

## ?? 總結

**核心概念：** 將全域共用的資源（如 PLC 連線）提升到應用程式最上層，避免因控件生命週期管理導致的資源重置問題。

**一句話總結：** 
> **全域資源放 MainWindow，Tab 內容只讀取，不建立。** ??
