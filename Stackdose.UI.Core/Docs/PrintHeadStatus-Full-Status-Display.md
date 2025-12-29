# ??? PrintHeadStatus 完整???示功能

## ? **改?完成摘要**

### **?示?容：**
1. **?度** - 1?（墨水?度）
2. **??** - 4?通道（V1, V2, V3, V4）
3. **??器** (Encoder)
4. **PrintIndex**

---

## ?? **UI 布局**

```
┌────────────────────────────────────────┐
│ PrintHead 1             【CONNECTED】   │
├────────────────────────────────────────┤
│ Config: feiyang_head1.json             │
│ Board: 192.168.22.68:10000             │
│ Model: Feiyang-M1536                   │
│                                        │
│ ┌──────────────────────────────────┐   │
│ │ ??? Temperature                   │   │
│ │       38.5°C                     │   │
│ └──────────────────────────────────┘   │
│                                        │
│ ┌──────────────────────────────────┐   │
│ │ ? Voltages                       │   │
│ │ V1: 23.5V  V2: 23.5V            │   │
│ │ V3: 23.5V  V4: 23.5V            │   │
│ └──────────────────────────────────┘   │
│                                        │
│ ┌──────────────────────────────────┐   │
│ │ ?? Encoder: 21000                │   │
│ └──────────────────────────────────┘   │
│                                        │
│ ┌──────────────────────────────────┐   │
│ │ ?? PrintIndex: 10000             │   │
│ └──────────────────────────────────┘   │
├────────────────────────────────────────┤
│ [  Connect  ]  [ Disconnect ]          │
└────────────────────────────────────────┘
```

---

## ?? **代???**

### **1?? XAML 布局 (`PrintHeadStatus.xaml`)**

```xml
<!-- Temperature -->
<Border Background="{DynamicResource Plc.Bg.Dark}">
    <StackPanel>
        <TextBlock Text="??? Temperature" FontSize="11"/>
        <TextBlock x:Name="TemperatureText" 
                   Text="N/A" 
                   FontSize="18" 
                   Foreground="#00d4ff"/>
    </StackPanel>
</Border>

<!-- Voltages (4 channels) -->
<Border Background="{DynamicResource Plc.Bg.Dark}">
    <StackPanel>
        <TextBlock Text="? Voltages" FontSize="11"/>
        <ItemsControl x:Name="VoltagesPanel">
            <ItemsControl.ItemsPanel>
                <UniformGrid Columns="2" Rows="2"/>
            </ItemsControl.ItemsPanel>
        </ItemsControl>
    </StackPanel>
</Border>

<!-- Encoder -->
<Border Background="{DynamicResource Plc.Bg.Dark}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="?? Encoder: "/>
        <TextBlock x:Name="EncoderText" Text="N/A"/>
    </StackPanel>
</Border>

<!-- PrintIndex -->
<Border Background="{DynamicResource Plc.Bg.Dark}">
    <StackPanel Orientation="Horizontal">
        <TextBlock Text="?? PrintIndex: "/>
        <TextBlock x:Name="PrintIndexText" Text="N/A"/>
    </StackPanel>
</Border>
```

### **2?? 假?据?示 (`ShowFakeTemperature`)**

```csharp
private void ShowFakeTemperature()
{
    // 假?度
    TemperatureText.Text = "38.5°C";
    
    // 假?? (4?通道)
    var fakeVoltages = new[] { 
        "V1: 23.5V", 
        "V2: 23.5V", 
        "V3: 23.5V", 
        "V4: 23.5V" 
    };
    VoltagesPanel.ItemsSource = fakeVoltages;
    
    // 假??器
    EncoderText.Text = "21000";
    
    // 假 PrintIndex
    PrintIndexText.Text = "10000";
}
```

### **3?? 真??据更新 (`UpdateStatusDisplay`)**

```csharp
private void UpdateStatusDisplay(dynamic status)
{
    try
    {
        // 1. ?度（墨水?度）
        if (status.InkTemperatureA != null)
        {
            float temp = (float)status.InkTemperatureA;
            if (temp >= 0 && temp <= 100)
            {
                TemperatureText.Text = $"{temp:F1}°C";
            }
        }

        // 2. ??（4?通道）
        var voltages = new List<string>();
        if (status.VoltageA != null) voltages.Add($"V1: {status.VoltageA:F1}V");
        if (status.VoltageB != null) voltages.Add($"V2: {status.VoltageB:F1}V");
        if (status.VoltageC != null) voltages.Add($"V3: {status.VoltageC:F1}V");
        if (status.VoltageD != null) voltages.Add($"V4: {status.VoltageD:F1}V");
        
        if (voltages.Count > 0)
        {
            VoltagesPanel.ItemsSource = voltages;
        }

        // 3. ??器
        if (status.Encoder != null)
        {
            EncoderText.Text = status.Encoder.ToString();
        }

        // 4. PrintIndex
        if (status.PrintIndex != null)
        {
            PrintIndexText.Text = status.PrintIndex.ToString();
        }
    }
    catch (Exception ex)
    {
        ComplianceContext.LogSystem(
            $"[PrintHead] UpdateStatusDisplay error: {ex.Message}",
            LogLevel.Warning
        );
    }
}
```

---

## ?? **??步?**

### **Step 1: ??正在?行的 WpfAppPrintHead**
```
???用程序，?放文件?定
```

### **Step 2: 重新构建?目**
```bash
dotnet build
```

### **Step 3: ?行 WpfAppPrintHead**
```bash
cd WpfAppPrintHead
dotnet run
```

### **Step 4: 查看未????（假?据）**
??看到：
```
??? Temperature
38.5°C

? Voltages
V1: 23.5V  V2: 23.5V
V3: 23.5V  V4: 23.5V

?? Encoder: 21000

?? PrintIndex: 10000
```

### **Step 5: ?? Connect 按?**
??成功后，?据?切??真?值。

---

## ? **常???排查**

### **?? 1：???示不正确**

**原因：**
- SDK 返回的字段名?可能不是 `VoltageA/B/C/D`

**解?方法：**
查看 SDK 的??字段名，可能是：
```csharp
status.BaseVoltage1
status.BaseVoltage2
status.BaseVoltage3
status.BaseVoltage4
```

修改 `UpdateStatusDisplay` 方法：
```csharp
if (status.BaseVoltage1 != null) voltages.Add($"V1: {status.BaseVoltage1:F1}V");
if (status.BaseVoltage2 != null) voltages.Add($"V2: {status.BaseVoltage2:F1}V");
if (status.BaseVoltage3 != null) voltages.Add($"V3: {status.BaseVoltage3:F1}V");
if (status.BaseVoltage4 != null) voltages.Add($"V4: {status.BaseVoltage4:F1}V");
```

### **?? 2：Encoder 或 PrintIndex ?示不正确**

**原因：**
- SDK 字段名?不匹配

**解?方法：**
在 `FeiyangPrintHead.cs` 中添加??日志：
```csharp
public object? GetStatus()
{
    var status = _native?.ReadStatusRaw();
    if (status != null)
    {
        LogInfo($"[DEBUG] Status fields: Encoder={status.Encoder}, PrintIndex={status.PrintIndex}");
    }
    return status;
}
```

---

## ?? **自定??色**

如果你想修改?色，可以?? XAML：

```xml
<!-- ?度?色 -->
<TextBlock Foreground="#00d4ff"/> <!-- 青色 -->

<!-- ???色 -->
<TextBlock Foreground="#ffd700"/> <!-- 金色 -->

<!-- ??器?色 -->
<TextBlock Foreground="#00ff7f"/> <!-- 春?色 -->

<!-- PrintIndex ?色 -->
<TextBlock Foreground="#ff69b4"/> <!-- 粉色 -->
```

---

## ?? **控件尺寸**

- **?度**: 320px
- **高度**: 340px
- **支持??**: 是（使用 `ScrollViewer`）

---

## ?? **下一步改?**

1. **??告警** - ?度/??超出范???示?色警告
2. **?史曲?** - 使用 LiveCharts ?示?度/????
3. **多噴頭支持** - 同??控多?噴頭??
4. **?据?出** - ?出???据到 CSV

---

**Created:** 2025-01-10  
**Status:** ? Ready for Testing (需要?? WpfAppPrintHead 后重新构建)  
**Next Step:** ???用 → 重新构建 → ???示
