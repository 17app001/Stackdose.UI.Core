# ?? Stackdose UI Templates - Complete Demo

## ? Demo Project Created Successfully!

已成功創建完整的示範專案 **Wpf.Demo**，展示如何使用 **Stackdose.UI.Templates** 的所有功能。

---

## ?? Demo Project Structure

```
Wpf.Demo/
├── Views/
│   ├── DemoHomePage.xaml         # 完整示範頁面
│   └── DemoHomePage.xaml.cs      # 事件處理邏輯
├── App.xaml                      # 應用程式入口（已合併顏色資源）
├── MainWindow.xaml               # 主視窗（使用 DemoHomePage）
└── Wpf.Demo.csproj              # 專案文件（已添加引用）
```

---

## ?? Demo 展示的功能

### 1?? **BasePage 的完整使用**
- ? 繼承自 `BasePage` 的自訂頁面
- ? 自動包含 Header、Navigation、BottomBar
- ? 透過 `ContentArea` 屬性定義主要內容
- ? 處理 `LogoutRequested` 和 `NavigationRequested` 事件

### 2?? **共用元件展示**
頁面自動包含：
- **AppHeader**：Logo、系統標題、頁面標題、使用者資訊、登出按鈕
- **LeftNavigation**：5 個導航項目（Home、Process、Log、User、Settings）
- **AppBottomBar**：版本號、日期時間、版權資訊

### 3?? **色彩系統展示**
展示所有共用顏色：
- **Primary**: #00d4ff（青藍色）
- **Success**: #2ecc71（綠色）
- **Warning**: #f39c12（橙色）
- **Error**: #e74c3c（紅色）

### 4?? **互動功能**
- 點擊左側導航項目 → 顯示導航事件
- 點擊右上角 Logout 按鈕 → 顯示登出事件
- 點擊示範按鈕 → 展示自訂事件處理

---

## ?? 如何運行 Demo

### 方式 1：Visual Studio
1. 開啟 `Wpf.Demo.csproj`
2. 按 `F5` 或點擊「開始偵錯」
3. 應用程式視窗將會顯示完整的示範頁面

### 方式 2：命令列
```bash
cd Wpf.Demo
dotnet run
```

---

## ?? Demo 頁面截圖說明

### 頁面布局：

```
┌──────────────────────────────────────────────────────────────┐
│  [S] STACKDOSE 3D PRINTING SYSTEM    Demo Home Page    [A] Admin [Logout]  │
├─────────┬────────────────────────────────────────────────────┤
│         │  Welcome to Stackdose UI Templates Demo!          │
│ Home    │  ───────────────────────────────────────────      │
│ Process │                                                     │
│ Log     │  ?? Shared Header   ?? Navigation   ?? Bottom Bar │
│ User    │                                                     │
│ Settings│  ───────────────────────────────────────────      │
│         │  Interactive Demo:                                │
│         │  [Click Me!] [Test Navigation] [Try Logout]       │
│         │                                                     │
│         │  Color System Preview:                            │
│         │  [Primary] [Success] [Warning] [Error]            │
├─────────┴────────────────────────────────────────────────────┤
│  v2.0.1  Build 2025.01.12    2025-01-14 14:30:25    ? 2025  │
└──────────────────────────────────────────────────────────────┘
```

---

## ?? 示範的設計模式

### 1. **繼承模式**
```csharp
public partial class DemoHomePage : BasePage
{
    public DemoHomePage()
    {
        InitializeComponent();
        PageTitle = "Demo Home Page";  // 設定頁面標題
    }
}
```

### 2. **事件處理**
```csharp
// 處理登出事件
private void OnLogout(object sender, RoutedEventArgs e)
{
    // 你的登出邏輯
}

// 處理導航事件
private void OnNavigate(object sender, NavigationItem e)
{
    // 導航到目標頁面
    // e.NavigationTarget 包含目標頁面名稱
}
```

### 3. **內容區域定義**
```xaml
<pages:BasePage.ContentArea>
    <Grid>
        <!-- 你的自訂內容 -->
    </Grid>
</pages:BasePage.ContentArea>
```

### 4. **使用共用顏色**
```xaml
<Border Background="{StaticResource PrimaryBrush}"/>
<TextBlock Foreground="{StaticResource SuccessBrush}"/>
```

---

## ?? 程式碼重點解說

### DemoHomePage.xaml 重點

#### 1. 引用 BasePage
```xaml
<pages:BasePage x:Class="Wpf.Demo.Views.DemoHomePage"
                xmlns:pages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
                PageTitle="Demo Home Page"
                LogoutRequested="OnLogout"
                NavigationRequested="OnNavigate">
```

#### 2. 合併顏色資源
```xaml
<pages:BasePage.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</pages:BasePage.Resources>
```

#### 3. 定義內容區域
```xaml
<pages:BasePage.ContentArea>
    <!-- 所有你的自訂內容都放在這裡 -->
</pages:BasePage.ContentArea>
```

---

## ?? 進階功能展示

### 自訂導航項目

如果需要自訂導航選單，可以在 code-behind 中：

```csharp
public DemoHomePage()
{
    InitializeComponent();
    
    // 獲取 LeftNavigation 控制項
    var leftNav = this.FindName("LeftNavigation") as LeftNavigation;
    if (leftNav != null)
    {
        // 自訂導航項目
        leftNav.NavigationItems = new ObservableCollection<NavigationItem>
        {
            new NavigationItem { Title = "儀表板", Subtitle = "Dashboard", NavigationTarget = "Dashboard", IsSelected = true },
            new NavigationItem { Title = "資料分析", Subtitle = "Analytics", NavigationTarget = "Analytics" },
            // ... 更多項目
        };
    }
}
```

### 動態更改頁面標題

```csharp
// 在任何時候更改頁面標題
this.PageTitle = "新的頁面標題";
```

---

## ?? 學習重點

通過這個 Demo，您將學到：

1. ? **如何使用 BasePage 快速創建一致的頁面**
2. ? **如何處理導航和登出事件**
3. ? **如何使用共用的色彩系統**
4. ? **如何在內容區域自訂佈局**
5. ? **如何整合到現有的 WPF 應用程式**

---

## ?? 下一步

### 創建更多頁面

根據這個示範，您可以輕鬆創建更多頁面：

```bash
# 創建新頁面
1. 複製 DemoHomePage.xaml 和 .cs
2. 重新命名為 DemoMachinePage.xaml
3. 修改 PageTitle 和 ContentArea
4. 在 MainWindow 中切換頁面
```

### 實作頁面導航

建議使用 MVVM 模式實作完整的導航系統：
- 使用 `Frame` 或 `ContentControl` 進行頁面切換
- 實作 `INotificationService` 處理導航邏輯
- 使用 `CommunityToolkit.Mvvm` 的 `RelayCommand`

---

## ?? 專案依賴

```xml
<ItemGroup>
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\Stackdose.UI.Core\Stackdose.UI.Core.csproj" />
  <ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
</ItemGroup>
```

---

## ? 編譯狀態

? **專案已成功編譯**  
? **無錯誤、無警告**  
? **可直接執行**

---

## ?? 總結

這個 Demo 展示了：

| 功能 | 狀態 | 說明 |
|------|------|------|
| **BasePage 使用** | ? | 完整展示繼承和使用 |
| **事件處理** | ? | Logout 和 Navigation 事件 |
| **色彩系統** | ? | 展示所有共用顏色 |
| **互動功能** | ? | 按鈕和導航項目點擊 |
| **自訂內容** | ? | ContentArea 完整示範 |

---

## ?? 需要協助？

如果您想：
- ?? 添加更多示範頁面
- ?? 自訂樣式和主題
- ?? 實作完整的 MVVM 架構
- ?? 添加更多共用元件

請隨時告訴我！

---

? 2025 Stackdose Inc. - UI Templates Demo
