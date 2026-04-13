# Stackdose.Tools.MachinePageDesigner — 架構書

> 版本：v1.0 | 日期：2026-04-01
> 本文件供開發者與 AI 理解整個設計器的設計動機、架構決策與實作規劃。
> 讀完本文件即可獨立開始任一模組的實作。

---

## 1. 專案定位

### 是什麼

`Stackdose.Tools.MachinePageDesigner`（以下簡稱 **Designer**）是一個獨立的 WPF 視覺化頁面設計工具，讓使用者以**拖拉方式**將 PLC 控制項組合成 MachinePage，並輸出為可被 DeviceFramework 直接讀取的 `.machinedesign.json` 設計檔。

### 不是什麼

- 不是自由畫布（非 Blend / Visual Studio Designer 風格）
- 不是 `ProjectGeneratorUI` 的一部分（獨立工具，可單獨安裝）
- 不直接產生 `.xaml` 檔案（透過 DeviceFramework 動態渲染）

### 核心設計哲學

> **「區塊決定版型，拖拉決定內容」**

頁面永遠由固定的**功能區塊（Zone）**組成，使用者只需專注在每個 Zone 裡放什麼控制項、怎麼排列。自動對齊與尺寸一致由 `UniformGrid` 保證，無需手動調整任何座標。

---

## 2. 架構決策說明

| 決策 | 選擇 | 理由 |
|---|---|---|
| 版面模型 | 區塊 Zone 制（非自由畫布） | 降低實作難度 80%；自動對齊；與既有 DeviceFramework 架構一致 |
| 區塊內排列 | `UniformGrid`（固定欄數） | 控制項自動等寬等高，零對齊問題 |
| 輸出格式 | `.machinedesign.json` | 可回頭編輯；DeviceFramework 已有 JSON 驅動機制可擴充 |
| 工具定位 | 獨立專案 | 既有專案可回頭修改；未來可交給客戶自行調整介面 |
| 渲染方式 | 執行時動態渲染 | 設計檔改了不需重新 Build |
| 控制項來源 | 直接使用 `Stackdose.UI.Core` 真實控制項 | 設計時所見 = 執行時所見（WYSIWYG） |

---

## 3. Solution 位置與相依關係

```
Stackdose.UI.Core.sln
└── Stackdose.Tools.MachinePageDesigner    ← 新增
      ├── 參考 Stackdose.UI.Core           （PlcLabel 等控制項）
      ├── 參考 Stackdose.App.DeviceFramework（PlcDataGridPanel、DeviceLabelViewModel）
      └── 不參考 ProjectGeneratorUI        （保持工具獨立性）
```

### 與其他工具的串接方式

```
ProjectGeneratorUI                      MachinePageDesigner
  產生新專案結構          <──────────>   開啟 / 編輯 .machinedesign.json
  若目錄有 .machinedesign.json           儲存設計檔
  → 包含進生成結果
```

串接點：一個 `.machinedesign.json` 設計檔，兩個工具都能讀寫。

---

## 4. 主視窗 UI 版型

```
┌─ Toolbar ─────────────────────────────────────────────────────────────────┐
│  [新建] [開啟] [儲存]    [預覽]    [頁面設定▾]                            │
├─ Toolbox ───┬─ Design Canvas ──────────────────────────┬─ Property Panel ─┤
│             │                                           │                  │
│ ── PLC ──   │  ┌── Zone: Command Operation ──────────┐ │  選取元件後顯示   │
│ PlcLabel    │  │  [Cmd Op Card]  ← 固定，不可編輯    │ │                  │
│ PlcText     │  └────────────────────────────────────── ┘ │  【PlcLabel】    │
│ PlcStatus-  │                                           │  標籤：溫度      │
│  Indicator  │  ┌── Zone: Live Data ──────────────────┐ │  地址：D100      │
│             │  │  ┌──────┐ ┌──────┐ ┌──────┐        │ │  數值大小：24    │
│ ── 按鈕 ──  │  │  │ PLC  │ │ PLC  │ │  +   │        │ │  外框形狀：○ ●  │
│ SecuredBtn  │  │  │Label │ │Label │ │      │        │ │  色彩主題：      │
│             │  │  └──────┘ └──────┘ └──────┘        │ │  [NeonBlue ▾]   │
│ ── 容器 ──  │  │  欄數：[2▾]  標題：Live Data        │ │                  │
│ Spacer      │  └────────────────────────────────────── ┘ │  預設值：0       │
│             │                                           │                  │
│             │  ┌── Zone: Device Status ──────────────┐ │                  │
│             │  │  ┌──────┐ ┌──────┐                  │ │                  │
│             │  │  │ PLC  │ │  +   │                  │ │                  │
│             │  │  │Label │ │      │                  │ │                  │
│             │  │  └──────┘ └──────┘                  │ │                  │
│             │  └────────────────────────────────────── ┘ │                  │
├─────────────┴──────────────────────────────────────────┴──────────────────┤
│ 狀態列：Design.machinedesign.json  |  已選取：PlcLabel D100  |  Zone: LiveData │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## 5. 核心概念定義

### 5.1 Zone（功能區塊）

Zone 是頁面上的一個固定功能區域，對應到 `DynamicDevicePage` 的現有分區。

| Zone ID | 顯示名稱 | 說明 | 可編輯內容 |
|---|---|---|---|
| `commandOp` | Command Operation | 製程按鈕區 | 固定（命令來自 config） |
| `liveData` | Live Data | 主要數值顯示 | ✅ 可自由配置控制項 |
| `deviceStatus` | Device Status | 狀態數值顯示 | ✅ 可自由配置控制項 |
| `liveLog` | System Log | 即時日誌 | 固定（開關即可） |
| `alarmViewer` | Alarm Viewer | 警報列表 | 固定（開關即可） |
| `sensorViewer` | Sensor Viewer | 感測器列表 | 固定（開關即可） |

> **設計原則**：Zone 的位置由頁面版型決定（SplitRight / SplitBottom / Standard），使用者只決定**可編輯 Zone 內放什麼**。

### 5.2 DesignerItem（設計項目）

Zone 內可放置的最小單位，包裝一個真實 WPF 控制項：

```
┌──────────────────────┐
│  ● 選取框            │  ← 選取時顯示藍框
│  ┌──────────────┐    │
│  │  真實控制項  │    │  ← 執行時完全一樣的渲染
│  │  (PlcLabel)  │    │
│  └──────────────┘    │
│  [↕] [×]             │  ← 拖拉排序、移除
└──────────────────────┘
```

設計時 PlcLabel 不連線 PLC，顯示 `DefaultValue`（例如 `"0"` 或 `"--"`）。

### 5.3 DesignDocument（設計文件）

整個頁面的資料模型，序列化為 `.machinedesign.json`。

---

## 6. 資料模型（.machinedesign.json）

### 完整 JSON Schema

```json
{
  "version": "1.0",
  "meta": {
    "title": "MyDevice Machine Page",
    "machineId": "M1",
    "createdAt": "2026-04-01T00:00:00Z",
    "modifiedAt": "2026-04-01T12:00:00Z"
  },
  "layout": {
    "mode": "SplitRight",
    "leftCommandWidthPx": 250,
    "rightColumnWidthStar": 0.85,
    "showLiveLog": true,
    "showAlarmViewer": true,
    "showSensorViewer": false
  },
  "zones": {
    "liveData": {
      "title": "Live Data",
      "columns": 2,
      "items": [
        {
          "id": "item-001",
          "type": "PlcLabel",
          "order": 0,
          "props": {
            "label": "溫度",
            "address": "D100",
            "defaultValue": "0",
            "valueFontSize": 24,
            "frameShape": "Rectangle",
            "valueColorTheme": "NeonBlue",
            "divisor": 10,
            "stringFormat": "F1"
          }
        },
        {
          "id": "item-002",
          "type": "PlcLabel",
          "order": 1,
          "props": {
            "label": "壓力",
            "address": "D101",
            "defaultValue": "0",
            "valueFontSize": 24,
            "frameShape": "Circle",
            "valueColorTheme": "NeonGreen"
          }
        },
        {
          "id": "item-003",
          "type": "Spacer",
          "order": 2,
          "props": {}
        }
      ]
    },
    "deviceStatus": {
      "title": "Device Status",
      "columns": 2,
      "items": [
        {
          "id": "item-010",
          "type": "PlcLabel",
          "order": 0,
          "props": {
            "label": "運轉時間",
            "address": "D200",
            "defaultValue": "0",
            "valueFontSize": 16,
            "frameShape": "Rectangle",
            "valueColorTheme": "White"
          }
        }
      ]
    }
  }
}
```

### 支援的 DesignerItem 類型（`type` 欄位）

| Type | 對應 UI.Core 控制項 | 主要 Props |
|---|---|---|
| `PlcLabel` | `PlcLabel` | address, label, valueFontSize, frameShape, valueColorTheme, defaultValue, divisor, stringFormat |
| `PlcText` | `PlcText` | address, label |
| `PlcStatusIndicator` | `PlcStatusIndicator` | displayAddress |
| `SecuredButton` | `SecuredButton`（DeviceFramework 包裝） | label, commandAddress, requiredLevel, theme |
| `Spacer` | 空白 Border | 無（僅佔位） |

---

## 7. 架構分層

```
Stackdose.Tools.MachinePageDesigner/
├── Models/
│   ├── DesignDocument.cs          ← 根資料模型（對應 JSON root）
│   ├── ZoneDefinition.cs          ← 單一 Zone 設定
│   ├── DesignerItemDefinition.cs  ← 單一控制項定義（type + props）
│   ├── PageLayoutConfig.cs        ← 版型設定（mode / widths / toggles）
│   └── DesignMeta.cs              ← 文件 metadata
│
├── ViewModels/
│   ├── MainViewModel.cs           ← 頂層 VM，協調各模組
│   ├── DesignCanvasViewModel.cs   ← 畫布狀態（選取、拖拉進行中）
│   ├── ZoneViewModel.cs           ← 單一 Zone 的項目清單、欄數、標題
│   ├── DesignerItemViewModel.cs   ← 單一項目 VM（props 雙向綁定）
│   ├── ToolboxViewModel.cs        ← 工具箱可用控制項清單
│   └── PropertyPanelViewModel.cs  ← 動態屬性面板（隨選取項目切換）
│
├── Views/
│   ├── MainWindow.xaml            ← 主視窗（Toolbar + 三欄布局）
│   ├── DesignCanvas.xaml          ← 畫布：組合各 ZoneView
│   ├── ZoneView.xaml              ← 單一 Zone 容器（UniformGrid + Drop target）
│   ├── DesignerItemView.xaml      ← 項目卡片（選取框 + 控制項 + 操作列）
│   ├── ToolboxPanel.xaml          ← 工具箱（拖拉來源）
│   └── PropertyPanel.xaml         ← 屬性面板（DataTemplateSelector 動態切換）
│
├── Controls/
│   └── DesignTimeControlFactory.cs ← 根據 DesignerItemDefinition 建立對應控制項實例
│
├── Services/
│   ├── DesignFileService.cs        ← Load / Save .machinedesign.json
│   ├── DesignRenderService.cs      ← DesignDocument → 執行時頁面（預覽用）
│   └── UndoRedoService.cs          ← 操作歷史（Ctrl+Z / Ctrl+Y）
│
├── Themes/
│   └── DesignerTheme.xaml          ← 選取框、Zone 邊框、工具箱樣式
│
└── App.xaml                        ← 合併 UI.Core Theme.xaml + DesignerTheme.xaml
```

---

## 8. 關鍵流程說明

### 8.1 拖拉控制項進入 Zone

```
[Toolbox] ─ MouseDown ──→ DragDrop.DoDragDrop(DataObject("ItemType", "PlcLabel"))
                                │
[ZoneView] ─ DragOver ──→ 顯示插入位置指示線（高亮 UniformGrid 的下一格）
                                │
            ─ Drop ─────→ ZoneViewModel.InsertItem("PlcLabel", dropIndex)
                                │
                         ← 建立 DesignerItemViewModel（含預設 props）
                                │
                         ← ZoneView ItemsControl 自動更新顯示
                                │
                         ← PropertyPanel 顯示新項目屬性供立即編輯
```

### 8.2 Zone 內拖拉換序

```
[DesignerItemView] ─ 拖拉把手 MouseDown ──→ 記錄拖拉起點 DesignerItemViewModel
                                              │
[ZoneView] ─ DragOver ──────────────────→  計算目標插入位置（index）
                                              │
            ─ Drop ──────────────────────→  ZoneViewModel.MoveItem(fromIdx, toIdx)
                                              │
                                         ← ObservableCollection 重排 → UI 自動更新
```

### 8.3 屬性編輯

```
使用者點選 DesignerItemView
    │
    ↓
DesignCanvasViewModel.SelectedItem = clicked DesignerItemViewModel
    │
    ↓
PropertyPanel ContentControl 的 DataTemplateSelector
  → 依 SelectedItem.ItemType 選擇對應 DataTemplate
    ├── PlcLabelPropertyTemplate.xaml
    ├── SecuredButtonPropertyTemplate.xaml
    └── SpacerPropertyTemplate.xaml（空，無屬性）
    │
    ↓
使用者修改屬性 → 雙向綁定直接更新 DesignerItemViewModel.Props
    │
    ↓
DesignerItemView 中的控制項重新建立（via DesignTimeControlFactory）
```

### 8.4 儲存與載入

```
儲存：
MainViewModel.SaveCommand
    → DesignFileService.Save(DesignDocument, filePath)
    → 序列化 JSON（System.Text.Json）
    → 寫入 .machinedesign.json

載入：
MainViewModel.OpenCommand
    → DesignFileService.Load(filePath)
    → 反序列化 JSON → DesignDocument
    → 建立各層 ViewModel（ZoneViewModel ← ZoneDefinition）
    → 建立各 DesignerItemViewModel（← DesignerItemDefinition）
    → UI 自動綁定顯示
```

### 8.5 執行時渲染（DeviceFramework 整合）

```
App 啟動
    → RuntimeHost 讀取 app-meta.json + Machine*.config.json
    → 若 config 有 "machineDesignFile": "M1.machinedesign.json"
    → DesignRenderService.Render(DesignDocument)
        ├── 建立 ZonePanel（for each zone）
        ├── 為每個 DesignerItemDefinition 建立真實控制項
        │     PlcLabel / PlcText / SecuredButton...
        ├── 套用 props（address, fontSize, frameShape...）
        └── 組合進 DynamicDevicePage 對應位置
```

---

## 9. ZoneView 實作重點

### XAML 結構

```xml
<Border x:Name="ZoneBorder" Style="{StaticResource ZoneCard}">
    <Grid>
        <!-- Zone 標題列 -->
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 標題 + 設定列 -->
            <RowDefinition Height="*"/>     <!-- 內容 -->
        </Grid.RowDefinitions>

        <!-- 標題 + 欄數設定 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal">
            <TextBox Text="{Binding Title}" .../>
            <ComboBox ItemsSource="{Binding ColumnOptions}"
                      SelectedItem="{Binding Columns}" .../>
        </StackPanel>

        <!-- 控制項 Grid（自動對齊核心） -->
        <ScrollViewer Grid.Row="1" AllowDrop="True"
                      Drop="OnDrop" DragOver="OnDragOver">
            <ItemsControl ItemsSource="{Binding Items}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <UniformGrid Columns="{Binding Columns}"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <local:DesignerItemView/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </Grid>
</Border>
```

### 為什麼用 UniformGrid

- **自動對齊**：不需要任何 x/y 座標，放進去就排好
- **大小一致**：每個 cell 自動等分 Zone 寬度
- **欄數可調**：`Columns` 屬性直接控制每列幾個
- **換序簡單**：只需改 `ObservableCollection` 順序

---

## 10. DesignTimeControlFactory

根據 `DesignerItemDefinition` 建立對應的真實 WPF 控制項實例（設計時版本）。

```csharp
public static UIElement Create(DesignerItemDefinition def)
{
    return def.Type switch
    {
        "PlcLabel" => new PlcLabel
        {
            Label         = def.Props.GetString("label"),
            Address       = def.Props.GetString("address"),
            DefaultValue  = def.Props.GetString("defaultValue", "0"),
            ValueFontSize = def.Props.GetDouble("valueFontSize", 18),
            FrameShape    = def.Props.GetString("frameShape", "Rectangle"),
            ValueForeground = def.Props.GetString("valueColorTheme", "NeonBlue"),
            ShowAddress   = false,
            // 設計時不連 PLC，顯示 DefaultValue
        },
        "PlcText" => new PlcText
        {
            Label   = def.Props.GetString("label"),
            Address = def.Props.GetString("address"),
        },
        "PlcStatusIndicator" => new PlcStatusIndicator
        {
            DisplayAddress = def.Props.GetString("displayAddress"),
        },
        "SecuredButton" => BuildSecuredButton(def),
        "Spacer" => new Border { Background = Brushes.Transparent },
        _ => new TextBlock { Text = $"未知類型: {def.Type}" }
    };
}
```

---

## 11. PropertyPanel 動態模板

使用 `DataTemplateSelector` 根據選取項目類型切換屬性面板：

```csharp
public class PropertyPanelTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PlcLabelTemplate   { get; set; }
    public DataTemplate? PlcTextTemplate    { get; set; }
    public DataTemplate? SecuredBtnTemplate { get; set; }
    public DataTemplate? SpacerTemplate     { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not DesignerItemViewModel vm) return null;
        return vm.ItemType switch
        {
            "PlcLabel"          => PlcLabelTemplate,
            "PlcText"           => PlcTextTemplate,
            "SecuredButton"     => SecuredBtnTemplate,
            "Spacer"            => SpacerTemplate,
            _                   => null
        };
    }
}
```

### PlcLabel 屬性面板欄位

| 屬性 | 控制項類型 | 說明 |
|---|---|---|
| `label` | TextBox | 標籤名稱 |
| `address` | TextBox | PLC 位址（D100、M200） |
| `valueFontSize` | Slider (8–72) + 數字顯示 | 數值字體大小 |
| `frameShape` | RadioButton：□ Rectangle、○ Circle | 外框形狀 |
| `valueColorTheme` | ComboBox | NeonBlue / NeonGreen / White / Warning / Error... |
| `defaultValue` | TextBox | 無 PLC 時顯示的預設值 |
| `divisor` | NumericUpDown | 顯示值 = PLC值 ÷ divisor |
| `stringFormat` | ComboBox | F0 / F1 / F2 |

---

## 12. UndoRedo 機制

採用 **Command Pattern + Stack**：

```csharp
public class UndoRedoService
{
    private readonly Stack<IDesignCommand> _undoStack = new();
    private readonly Stack<IDesignCommand> _redoStack = new();

    public void Execute(IDesignCommand cmd) { cmd.Execute(); _undoStack.Push(cmd); _redoStack.Clear(); }
    public void Undo() { if (_undoStack.TryPop(out var cmd)) { cmd.Undo(); _redoStack.Push(cmd); } }
    public void Redo() { if (_redoStack.TryPop(out var cmd)) { cmd.Execute(); _undoStack.Push(cmd); } }
}

// 範例 Command
public class AddItemCommand : IDesignCommand
{
    private readonly ZoneViewModel _zone;
    private readonly DesignerItemViewModel _item;
    private readonly int _index;

    public void Execute() => _zone.Items.Insert(_index, _item);
    public void Undo()    => _zone.Items.Remove(_item);
}
```

---

## 13. 實作階段規劃

### Phase 1：基礎骨架（可拖拉但無持久化）

**目標**：能拖拉控制項進 Zone，能看到真實控制項預覽，能選取後看到屬性面板。

- [ ] 建立專案與 .csproj 相依設定
- [ ] App.xaml 合併主題資源
- [ ] `DesignDocument` / `ZoneDefinition` / `DesignerItemDefinition` Models
- [ ] `ZoneViewModel` + `DesignerItemViewModel`
- [ ] `MainWindow.xaml` 三欄骨架
- [ ] `ToolboxPanel.xaml`（靜態清單，支援 DragDrop 起點）
- [ ] `ZoneView.xaml`（UniformGrid + Drop target）
- [ ] `DesignerItemView.xaml`（卡片 + 選取框）
- [ ] `DesignTimeControlFactory.cs`（PlcLabel / Spacer 先做）
- [ ] `PropertyPanel.xaml`（PlcLabel 屬性先做）

**完成條件**：從 Toolbox 拖 PlcLabel 進 LiveData Zone，設定 Address 後能看到 PlcLabel 顯示 DefaultValue。

---

### Phase 2：完整控制項 + 持久化

**目標**：所有控制項類型都支援，能存檔開檔。

- [ ] `DesignFileService`（JSON 序列化 / 反序列化）
- [ ] 儲存 / 開啟 / 新建 Command（MainViewModel）
- [ ] 補完所有 DesignerItem 類型（PlcText / SecuredButton / PlcStatusIndicator）
- [ ] 對應 PropertyPanel 模板
- [ ] Zone 欄數設定 UI（ComboBox 1~4 欄）
- [ ] Zone 標題可編輯
- [ ] Zone 內拖拉換序（ItemsControl 內部 DragDrop）

**完成條件**：設計完整頁面，儲存後重開能完整還原。

---

### Phase 3：UndoRedo + 版型選擇

**目標**：提升易用性。

- [ ] `UndoRedoService` + Ctrl+Z / Ctrl+Y
- [ ] 頁面版型切換（SplitRight / SplitBottom / Standard）
- [ ] DesignCanvas 即時反映版型變化
- [ ] 多選（Shift+點選）+ 群組刪除

---

### Phase 4：DeviceFramework 整合

**目標**：設計檔能被真實 App 讀取渲染。

- [ ] `DesignRenderService`（在 DeviceFramework 內）
- [ ] `MachineConfig` 加 `MachineDesignFile` 屬性
- [ ] `DeviceContextMapper` 優先讀 `.machinedesign.json`
- [ ] `DynamicDevicePage` 能交給 `DesignRenderService` 渲染
- [ ] ProjectGeneratorUI 生成時若有設計檔，複製進新專案

---

### Phase 5：進階功能（可選）

- [ ] 即時預覽視窗（連上 PLC 後顯示真實數值）
- [ ] Zone 自訂（新增 / 移除 Zone）
- [ ] 匯出為 XAML（給需要靜態頁面的情境）
- [ ] 多機台設計（切換 Machine Tab）

---

## 14. 注意事項與風險

| 風險 | 說明 | 緩解方式 |
|---|---|---|
| PlcLabel 設計時嘗試連線 PLC | 設計器環境沒有 PLC | `DesignTimeControlFactory` 建立控制項後，確認 PlcContext 未初始化，PlcLabel 會自動顯示 DefaultValue，不需特別處理 |
| 主題資源找不到 | 同 ProjectGeneratorUI 踩到的 `Surface.Bg.Card` 問題 | App.xaml 最前面合併 `UI.Core/Themes/Theme.xaml` |
| Zone 內 UniformGrid Columns 不支援 Binding | WPF UniformGrid.Columns 是 DP，支援 Binding | 需確認 ItemsPanelTemplate 內的 Binding 語法正確（ElementName 或 TemplatedParent） |
| .machinedesign.json Schema 版本演進 | 未來欄位增加可能破壞舊設計檔 | `version` 欄位 + 讀取時做欄位預設值處理（JsonIgnore + 初始值） |

---

## 15. 30 秒摘要（給交接 / AI）

`MachinePageDesigner` 是一個獨立 WPF 工具，讓使用者以拖拉方式把 `PlcLabel`、`PlcText` 等真實控制項放進固定的頁面區塊（Zone），自動對齊不需手動調座標。設計結果存為 `.machinedesign.json`，可被 `DeviceFramework` 在執行時動態渲染成真實頁面。

實作核心：**Zone 用 `UniformGrid` 自動排列 → DesignerItem 包裝真實控制項 → PropertyPanel 動態編輯屬性 → DesignFileService 持久化**。

開始實作請從 **Phase 1** 的 `ZoneView.xaml` + `DesignerItemView.xaml` 下手。
