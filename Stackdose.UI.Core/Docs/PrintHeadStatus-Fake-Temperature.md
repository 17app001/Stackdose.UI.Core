# ??? PrintHeadStatus 安??ボ

## ? **?**

?よ獽 UI ガЫ??`PrintHeadStatus` 北ン??????ボ**安??誹**ㄏ糛繷??Τ??

---

## ?? **?ボ狦**

### **ゼ????DISCONNECTED**

```

 PrintHead 1          DISCONNECTED  

 Config: feiyang_head1.json             
 Board: 192.168.22.68:10000             
 Model: Feiyang-M1536                   
                                        
 ??? Temperatures                        
         
  CH1: 38.5C   CH2: 39.2C    ■ 安?
         

 [  Connect  ]  [ Disconnect ]          

```

### **??ΘCONNECTED**

???ち??**痷???誹**– 500ms 穝Ω

---

## ?? **???**

### **1?? 北ン???ボ安?**

```csharp
private async void OnControlLoaded(object sender, RoutedEventArgs e)
{
    // 更皌竚郎
    if (!LoadConfiguration())
    {
        return;
    }

    // ? ?ボ安?ノ UI ??
    ShowFakeTemperature();

    // 笆硈絬狦币ノ
    if (AutoConnect)
    {
        await Task.Delay(500);
        await ConnectAsync();
    }
}
```

### **2?? 安??誹ネΘ**

```csharp
/// <summary>
/// ?ボ安??誹ノ UI ??
/// </summary>
private void ShowFakeTemperature()
{
    var fakeTemps = new float[] { 38.5f, 39.2f };
    UpdateTemperatureDisplay(fakeTemps);

    ComplianceContext.LogSystem(
        "[PrintHead] ?? Displaying fake temperature data for UI testing",
        LogLevel.Info,
        showInUi: false
    );
}
```

### **3?? ??Θ?ち??痷??**

```csharp
private async Task ConnectAsync()
{
    // ... ???? ...

    if (connected)
    {
        _isConnected = true;
        UpdateStatus(true, "CONNECTED");

        // ? ??痷???北?滦?安?
        StartTemperatureMonitoring();
    }
}
```

---

## ?? **??˙?**

### **Step 1: ?︽ WpfAppPrintHead**

```bash
cd WpfAppPrintHead
dotnet run
```

### **Step 2: 琩ゼ????**

??ミ??ボ
```
??? Temperatures
CH1: 38.5C  CH2: 39.2C
```

### **Step 3: ?? Connect ?**

??Θ??ち??痷??誹?穝

---

## ?? **﹚?安?**

狦稱э安??惠?? `ShowFakeTemperature()` よ猭

```csharp
private void ShowFakeTemperature()
{
    // ﹚?安??
    var fakeTemps = new float[] { 
        25.0f,  // CH1: 茎?
        30.0f   // CH2: 糛繷?
    };
    
    UpdateTemperatureDisplay(fakeTemps);
}
```

---

## ?? **??**

### **1?? ??安?家???て**

狦稱?安????て家?痷??春??э

```csharp
private void ShowFakeTemperature()
{
    var random = new Random();
    var fakeTemps = new float[] { 
        38.0f + (float)random.NextDouble() * 2.0f,  // 38.0 ~ 40.0C
        39.0f + (float)random.NextDouble() * 2.0f   // 39.0 ~ 41.0C
    };
    
    UpdateTemperatureDisplay(fakeTemps);
}
```

### **2?? 窽ノ安?????ボ**

狦ぃ稱?ボ安?惠猔?奔?︽

```csharp
private async void OnControlLoaded(object sender, RoutedEventArgs e)
{
    if (!LoadConfiguration())
    {
        return;
    }

    // ShowFakeTemperature(); // ■ 猔?奔?︽

    if (AutoConnect)
    {
        await Task.Delay(500);
        await ConnectAsync();
    }
}
```

---

## ? **涩?**

1. **UI ??ね** - ぃ惠璶??糛繷碞??ボ狦
2. **????** - е硉????办ガЫ㎝?Α
3. **簍ボ家Α** - ノ?珇簍ボ┪ゅ?篒?

---

## ?? **猔種ㄆ?**

1. **安??ゼ????ボ** - ??Θ??ち??痷??誹
2. **ぃ?紇?痷?** - 安?ぃ??らв┪?誹?
3. **??らв??** - 安??らвい??? `?? fake temperature`

---

**Created:** 2025-01-10  
**Status:** ? Complete  
**Purpose:** UI Layout Testing
