# Stackdose.UI.Templates

A reusable WPF UI component library for Stackdose 3D Printing System.

## ?? Components

### Controls

#### 1. **AppHeader**
Shared header component with logo, system title, page title, user info, and **window control buttons**.

**Properties:**
- `PageTitle` (string): Current page title
- `UserName` (string): Logged-in user name
- `UserRole` (string): User role/description

**Events:**
- `LogoutClicked`: Triggered when logout button is clicked
- `MinimizeClicked`: Triggered when minimize button is clicked
- `CloseClicked`: Triggered when close button is clicked

**Usage:**
```xaml
<controls:AppHeader PageTitle="My Page"
                   UserName="Admin"
                   UserRole="Administrator"
                   LogoutClicked="OnLogout"
                   MinimizeClicked="OnMinimize"
                   CloseClicked="OnClose"/>
```

---

#### 2. **AppBottomBar**
Shared bottom bar with version info, date/time, and copyright.

**Usage:**
```xaml
<controls:AppBottomBar/>
```

---

#### 3. **LeftNavigation**
Left navigation menu with customizable items.

**Properties:**
- `NavigationItems` (ObservableCollection<NavigationItem>): Navigation item list

**Events:**
- `NavigationRequested`: Triggered when navigation item is clicked

**Usage:**
```xaml
<controls:LeftNavigation NavigationRequested="OnNavigationRequested"/>
```

---

### Pages

#### **BasePage**
Base page template combining header, navigation, content area, and bottom bar.

**Properties:**
- `PageTitle` (string): Page title shown in header
- `ContentArea` (object): Main content to display

**Events:**
- `LogoutRequested`: Forwarded from AppHeader
- `MinimizeRequested`: Forwarded from AppHeader
- `CloseRequested`: Forwarded from AppHeader
- `NavigationRequested`: Forwarded from LeftNavigation

**Usage:**
```xaml
<pages:BasePage PageTitle="My Page"
                LogoutRequested="OnLogout"
                MinimizeRequested="OnMinimize"
                CloseRequested="OnClose"
                NavigationRequested="OnNavigate">
    <pages:BasePage.ContentArea>
        <!-- Your content -->
    </pages:BasePage.ContentArea>
</pages:BasePage>
```

---

## ?? Resources

### CommonColors.xaml
Centralized color system with:

**Primary Colors:**
- `PrimaryColor` / `PrimaryBrush`: #00d4ff (Cyan)
- `PrimaryDarkColor`: #0099cc

**Background Colors:**
- `BackgroundColor`: #1a1a2e (Dark Blue-Black)
- `CardBackgroundColor`: #16213e (Dark Blue)
- `DarkBackgroundColor`: #0f3460
- `HoverBackgroundColor`: #1e2a47

**Text Colors:**
- `TextPrimaryColor`: #FFFFFF (White)
- `TextSecondaryColor`: #95a5a6 (Gray)
- `TextTertiaryColor`: #7f8c8d

**Status Colors:**
- `SuccessColor`: #2ecc71 (Green)
- `WarningColor`: #f39c12 (Orange)
- `ErrorColor`: #e74c3c (Red)
- `InfoColor`: #3498db (Blue)

---

## ?? Getting Started

### 1. Add Project Reference

In your WPF application project (.csproj):

```xml
<ItemGroup>
  <ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
</ItemGroup>
```

### 2. Add Namespace

In your XAML file:

```xaml
xmlns:controls="clr-namespace:Stackdose.UI.Templates.Controls;assembly=Stackdose.UI.Templates"
xmlns:pages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
```

### 3. Merge Color Resources

In App.xaml:

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 4. Use BasePage

Create a new page using BasePage:

```xaml
<pages:BasePage x:Class="YourApp.Views.HomePage"
                xmlns:pages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
                PageTitle="Home Page">
    <pages:BasePage.ContentArea>
        <Grid>
            <!-- Your content here -->
        </Grid>
    </pages:BasePage.ContentArea>
</pages:BasePage>
```

---

## ?? Architecture

```
Stackdose.UI.Templates/
¢u¢w¢w Controls/
¢x   ¢u¢w¢w AppHeader.xaml          # Shared header component
¢x   ¢u¢w¢w AppBottomBar.xaml       # Shared bottom bar
¢x   ¢|¢w¢w LeftNavigation.xaml     # Left navigation menu
¢u¢w¢w Pages/
¢x   ¢|¢w¢w BasePage.xaml           # Base page template
¢u¢w¢w Styles/
¢x   ¢|¢w¢w (Future: Button/Card styles)
¢u¢w¢w Resources/
¢x   ¢|¢w¢w CommonColors.xaml       # Shared color system
¢u¢w¢w Converters/
¢x   ¢|¢w¢w FirstCharConverter.cs   # String to first char
¢|¢w¢w README.md
```

---

## ?? Benefits

? **Consistent UI**: All pages share the same header, navigation, and footer  
? **Easy Maintenance**: Update components in one place  
? **Reusable**: Can be referenced by multiple WPF projects  
? **Clean Architecture**: Separation of concerns  
? **Theme System**: Centralized color management  

---

## ?? Future Enhancements

- [ ] Button styles library
- [ ] Card/Panel styles
- [ ] Custom control templates
- [ ] Animation library
- [ ] Icon font integration
- [ ] Theme switcher (Light/Dark mode)

---

## ?? License

? 2025 Stackdose Inc. All Rights Reserved.
