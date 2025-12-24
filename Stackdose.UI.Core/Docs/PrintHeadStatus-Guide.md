# PrintHeadStatus 控制項 - 使用指南

## ?? 概述

`PrintHeadStatus` 是一個**拖拉式 WPF 控制項**，用於管理和監控飛揚（Feiyang）噴印頭的連線狀態和運行參數。

## ? 功能特點

- ? 讀取 JSON 配置檔（支援完整的 Feiyang 配置格式）
- ? 連線/斷線按鈕控制
- ? 即時顯示連線狀態
- ? 即時溫度監控（4 通道，每 200ms 更新）
- ? 顯示板卡 IP 和配置資訊
- ? 自動連線選項
- ? 符合 FDA 審計追蹤（Compliance Context 整合）
- ? 支援多個 PrintHead 實例（透過 PrintHeadContext）

---

## ?? 快速開始

### 1. 在 XAML 中添加控制項

```xml
<Window xmlns:Controls="http://schemas.stackdose.com/wpf">
    <Grid>
        <Controls:PrintHeadStatus x:Name="PrintHead1"
                                  HorizontalAlignment="Left" 
                                  Margin="73,480,0,0" 
                                  VerticalAlignment="Top"
                                  HeadName="Feiyang Head 1"
                                  ConfigFilePath="feiyang_head1.json"
                                  AutoConnect="False"/>
    </Grid>
</Window>
```

### 2. 創建配置檔（`feiyang_head1.json`）

```json
{
  "DriverType": "Feiyang",
  "Model": "Feiyang-M1536",
  "Enabled": true,
  "MachineType": "A",
  "HeadIndex": 0,
  "Name": "A-Head1",
  "BoardIP": "192.168.22.68",
  "BoardPort": 10000,
  "PcIP": "192.168.22.1",
  "PcPort": 10000,
  "Waveform": "A8_1536GS_L_25PL_UV_DROP1_30K_ABC0.data",
  "Firmware": {
    "MachineType": "M1536",
    "JetColors": [ 0, 0, 0, 0 ],
    "BaseVoltages": [ 35.0, 35.0, 35.0, 35.0 ],
    "OffsetVoltages": [ 0.0, 0.0, 0.0, 0.0 ],
    "HeatTemperature": 40.0,
    "DisableColumnMask": 0,
    "PrintheadColorCount": 1,
    "InstallDirectionPositive": false,
    "EncoderFunction": 0
  },
  "PrintMode": {
    "PrintDirection": "LeftToRight",
    "GratingDpi": 1270,
    "ImageDpi": 600,
    "GrayScale": 0,
    "GrayScaleDrop": 1,
    "ResetEncoder": 1000,
    "LColumnCali": [ 29.9, 22.3, 7.6 ],
    "RColumnCali": [ 7.6, 22.3, 29.9 ],
    "CaliPixelMM": 8
  }
}
```

### 3. 訂閱事件（可選）

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 訂閱連線成功事件
        PrintHead1.ConnectionEstablished += OnPrintHeadConnected;

        // 訂閱連線失敗/斷線事件
        PrintHead1.ConnectionLost += OnPrintHeadConnectionLost;
    }

    private void OnPrintHeadConnected()
    {
        ComplianceContext.LogSystem(
            "[App] PrintHead connected successfully!",
            LogLevel.Success,
            showInUi: true
        );
    }

    private void OnPrintHeadConnectionLost(string errorMessage)
    {
        ComplianceContext.LogSystem(
            $"[App] PrintHead connection lost: {errorMessage}",
            LogLevel.Error,
            showInUi: true
        );
    }
}
```

---

## ??? 屬性 (Dependency Properties)

| 屬性 | 類型 | 預設值 | 說明 |
|------|------|-------|------|
| `ConfigFilePath` | `string` | `"feiyang_head1.json"` | 配置檔路徑 |
| `HeadName` | `string` | `"PrintHead 1"` | 噴印頭顯示名稱 |
| `AutoConnect` | `bool` | `false` | 是否在控制項載入時自動連線 |

### 使用範例

```xml
<!-- 自動連線 -->
<Controls:PrintHeadStatus HeadName="主噴印頭"
                          ConfigFilePath="head_main.json"
                          AutoConnect="True"/>

<!-- 手動連線 -->
<Controls:PrintHeadStatus HeadName="備用噴印頭"
                          ConfigFilePath="head_backup.json"
                          AutoConnect="False"/>
```

---

## ?? UI 結構

### 控制項外觀

```
┌─────────────────────────────────────────┐
│  Feiyang Head 1              CONNECTED  │
├─────────────────────────────────────────┤
│  Config: feiyang_head1.json             │
│  Board:  192.168.22.68:10000            │
│  Model:  Feiyang-M1536                  │
│                                          │
│  ??? Temperatures                        │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐           │
│  │CH1:│ │CH2:│ │CH3:│ │CH4:│           │
│  │38.5│ │39.2│ │38.8│ │39.0│           │
│  └────┘ └────┘ └────┘ └────┘           │
├─────────────────────────────────────────┤
│  [ Connect ]      [ Disconnect ]        │
└─────────────────────────────────────────┘
```

### 狀態顏色

| 狀態 | 顏色 | 說明 |
|------|------|------|
| **DISCONNECTED** | ?? 紅色 (`#ff4757`) | 未連線 |
| **CONNECTING...** | ?? 黃色 (`#FFC107`) | 連線中 |
| **CONNECTED** | ?? 綠色 (`#2ecc71`) | 已連線 |

---

## ?? 溫度監控

### 自動監控

控制項連線成功後，會自動啟動溫度監控：
- **更新頻率**: 每 200ms
- **顯示格式**: `CH1: 38.5°C`
- **通道數量**: 4 個通道

### 監控邏輯

```csharp
private void StartTemperatureMonitoring()
{
    Task.Run(async () =>
    {
        while (!token.IsCancellationRequested && _isConnected)
        {
            // 讀取溫度
            var temps = _printHead?.GetTemperatures();

            // 更新 UI
            Dispatcher.Invoke(() =>
            {
                UpdateTemperatureDisplay(temps);
            });

            await Task.Delay(200, token); // 每 200ms 更新
        }
    }, token);
}
```

---

## ?? 整合 FeiyangPrintHead

### TODO: 實際整合步驟

目前控制項已預留整合點，需要替換以下部分：

#### 1. 連線邏輯

**目前（佔位符）**:
```csharp
// TODO: 實際連線邏輯
// _printHead = new FeiyangPrintHead(ConfigFilePath);
// bool connected = _printHead.Connect();

// 模擬連線
await Task.Delay(1000);
bool connected = true;
```

**應改為**:
```csharp
using Stackdose.PrintHead.Feiyang;

// 實際連線邏輯
_printHead = new FeiyangPrintHead(ConfigFilePath);
bool connected = _printHead.Connect();

if (connected)
{
    _printHead.ConfigurePrintMode();
}
```

#### 2. 溫度讀取

**目前（模擬數據）**:
```csharp
// 模擬溫度數據
var temps = new[] { 38.5, 39.2, 38.8, 39.0 };
```

**應改為**:
```csharp
// 實際讀取溫度
var temps = _printHead?.GetTemperatures();
```

#### 3. 斷線邏輯

**目前（佔位符）**:
```csharp
// TODO: 實際斷線邏輯
// _printHead?.Disconnect();
```

**應改為**:
```csharp
_printHead?.Disconnect();
```

---

## ?? 全域管理 (PrintHeadContext)

### 註冊 PrintHead

控制項連線成功後，會自動註冊到全域上下文：

```csharp
// 在 PrintHeadStatus.xaml.cs 中
if (connected)
{
    PrintHeadContext.RegisterPrintHead(_config.Name, _printHead);
}
```

### 取得 PrintHead 實例

其他地方可以透過 `PrintHeadContext` 取得 PrintHead：

```csharp
// 取得主要 PrintHead
var mainHead = PrintHeadContext.MainPrintHead;

// 取得指定名稱的 PrintHead
var head1 = PrintHeadContext.GetPrintHead("A-Head1");

// 檢查是否有已連線的 PrintHead
if (PrintHeadContext.HasConnectedPrintHead)
{
    // 執行操作...
}
```

### 訂閱全域事件

```csharp
// 訂閱任何 PrintHead 連線事件
PrintHeadContext.PrintHeadConnected += (name) =>
{
    Console.WriteLine($"PrintHead connected: {name}");
};

// 訂閱任何 PrintHead 斷線事件
PrintHeadContext.PrintHeadDisconnected += (name) =>
{
    Console.WriteLine($"PrintHead disconnected: {name}");
};
```

---

## ?? 配置檔格式說明

### 根屬性

| 欄位 | 類型 | 說明 | 範例 |
|------|------|------|------|
| `DriverType` | `string` | 驅動類型 | `"Feiyang"` |
| `Model` | `string` | 型號 | `"Feiyang-M1536"` |
| `Enabled` | `bool` | 是否啟用 | `true` |
| `Name` | `string` | 噴印頭名稱 | `"A-Head1"` |
| `BoardIP` | `string` | 板卡 IP | `"192.168.22.68"` |
| `BoardPort` | `int` | 板卡端口 | `10000` |
| `PcIP` | `string` | PC IP | `"192.168.22.1"` |
| `PcPort` | `int` | PC 端口 | `10000` |
| `Waveform` | `string` | 波形檔案 | `"A8_1536GS_L_25PL_UV_DROP1_30K_ABC0.data"` |

### Firmware 配置

| 欄位 | 類型 | 說明 |
|------|------|------|
| `MachineType` | `string` | 機型 |
| `JetColors` | `int[]` | 噴頭顏色（4 通道） |
| `BaseVoltages` | `double[]` | 基準電壓（4 通道） |
| `OffsetVoltages` | `double[]` | 偏移電壓（4 通道） |
| `HeatTemperature` | `double` | 加熱溫度 |
| `DisableColumnMask` | `int` | 禁用列遮罩 |
| `PrintheadColorCount` | `int` | 噴頭顏色數量 |

### PrintMode 配置

| 欄位 | 類型 | 說明 |
|------|------|------|
| `PrintDirection` | `string` | 列印方向（`"LeftToRight"`/`"RightToLeft"`） |
| `GratingDpi` | `int` | 光柵 DPI |
| `ImageDpi` | `int` | 圖像 DPI |
| `GrayScale` | `int` | 灰階等級 |
| `LColumnCali` | `double[]` | 左列校正數據 |
| `RColumnCali` | `double[]` | 右列校正數據 |
| `CaliPixelMM` | `double` | 校正像素/毫米 |

---

## ?? 工作流程

### 完整流程圖

```
1. 應用程式啟動
   ↓
2. PrintHeadStatus 控制項載入
   ↓
3. 讀取 JSON 配置檔
   ├─ 成功 → 顯示配置資訊
   └─ 失敗 → 顯示錯誤訊息
   ↓
4. 使用者點擊 Connect 按鈕（或 AutoConnect=true）
   ↓
5. 連線到噴印頭
   ├─ 成功 → 狀態變為 CONNECTED
   │   ↓
   │   配置列印模式 (ConfigurePrintMode)
   │   ↓
   │   註冊到 PrintHeadContext
   │   ↓
   │   啟動溫度監控（每 200ms）
   │   ↓
   │   觸發 ConnectionEstablished 事件
   │
   └─ 失敗 → 顯示錯誤訊息
       ↓
       觸發 ConnectionLost 事件
   ↓
6. 溫度持續監控並更新 UI
   ↓
7. 使用者點擊 Disconnect 按鈕
   ↓
8. 停止溫度監控
   ↓
9. 斷開連線
   ↓
10. 從 PrintHeadContext 移除
    ↓
11. 狀態變為 DISCONNECTED
```

---

## ?? 日誌記錄

### ComplianceContext 整合

所有重要操作都會記錄到 `ComplianceContext`：

```
[PrintHead] Config loaded: A-Head1 (Feiyang-M1536)
[PrintHead] Connecting to A-Head1 (192.168.22.68:10000)...
[PrintHead] Connection established: A-Head1
[PrintHead] Disconnected: A-Head1
[PrintHead] Temperature read error: Connection timeout
```

### 日誌等級

| 操作 | 日誌等級 |
|------|---------|
| 配置載入 | `Info` |
| 連線成功 | `Success` |
| 連線失敗 | `Error` |
| 溫度讀取錯誤 | `Warning` |
| 斷線 | `Info` |

---

## ?? 測試步驟

### 1. 基本連線測試

```
1. 啟動 WpfApp1
2. 確認 PrintHeadStatus 顯示配置資訊
3. 點擊 Connect 按鈕
4. 確認狀態變為 CONNECTED（綠色）
5. 確認溫度數據開始更新
```

### 2. 斷線測試

```
1. 點擊 Disconnect 按鈕
2. 確認狀態變為 DISCONNECTED（紅色）
3. 確認溫度更新停止
```

### 3. 自動連線測試

```xml
<Controls:PrintHeadStatus AutoConnect="True"/>
```

```
1. 啟動應用程式
2. 確認自動連線
3. 確認狀態變為 CONNECTED
```

### 4. 多 PrintHead 測試

```xml
<Controls:PrintHeadStatus x:Name="Head1" 
                          HeadName="Head 1"
                          ConfigFilePath="head1.json"/>

<Controls:PrintHeadStatus x:Name="Head2" 
                          HeadName="Head 2"
                          ConfigFilePath="head2.json"/>
```

---

## ?? 使用範例

### 範例 1：單一噴印頭

```xml
<Window xmlns:Controls="http://schemas.stackdose.com/wpf">
    <Grid>
        <Controls:PrintHeadStatus HeadName="Main PrintHead"
                                  ConfigFilePath="main_head.json"
                                  AutoConnect="True"/>
    </Grid>
</Window>
```

### 範例 2：多個噴印頭

```xml
<StackPanel>
    <Controls:PrintHeadStatus HeadName="Head A"
                              ConfigFilePath="head_a.json"/>

    <Controls:PrintHeadStatus HeadName="Head B"
                              ConfigFilePath="head_b.json"
                              Margin="0,10,0,0"/>

    <Controls:PrintHeadStatus HeadName="Head C"
                              ConfigFilePath="head_c.json"
                              Margin="0,10,0,0"/>
</StackPanel>
```

### 範例 3：整合 Recipe 系統

```csharp
// 在 Recipe 下載完成後，啟動噴印頭
RecipeContext.RecipeLoaded += (sender, recipe) =>
{
    var printHead = PrintHeadContext.MainPrintHead;
    if (printHead != null)
    {
        // 開始列印
        printHead.StartPrint();
    }
};
```

---

## ?? 未來規劃

### 即將實現的功能

- [ ] 實際整合 `FeiyangPrintHead` 類別
- [ ] 支援圖像載入和列印控制
- [ ] 噴頭狀態詳細顯示（噴嘴狀態、墨水量）
- [ ] 自動重連機制（類似 PlcWatcher）
- [ ] 配置檔編輯器
- [ ] 列印任務管理
- [ ] 錯誤診斷工具

---

## ?? 總結

### 主要特點

- ? **拖拉式控制項**：與 PlcStatus 相同的使用方式
- ? **JSON 配置**：支援完整的 Feiyang 配置格式
- ? **即時監控**：溫度、狀態即時更新
- ? **全域管理**：PrintHeadContext 統一管理
- ? **事件驅動**：支援連線/斷線事件訂閱
- ? **審計追蹤**：所有操作記錄到 ComplianceContext

### 當前狀態

**控制項架構已完成**，包括：
- 配置模型（PrintHeadConfig）
- UI 控制項（PrintHeadStatus.xaml）
- 連線邏輯框架
- 溫度監控框架
- 全域管理（PrintHeadContext）

**待整合**：
- FeiyangPrintHead 實際實例化
- GetTemperatures() 實際實現
- Connect/Disconnect 實際實現

**現在您可以開始整合實際的 FeiyangPrintHead 功能了！** ??
