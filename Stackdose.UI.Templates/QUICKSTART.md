# Quick Start

## 1) Add project reference

```xml
<ItemGroup>
  <ProjectReference Include="..\Stackdose.UI.Templates\Stackdose.UI.Templates.csproj" />
</ItemGroup>
```

## 2) Merge resources in `App.xaml`

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="/Stackdose.UI.Templates;component/Resources/CommonColors.xaml"/>
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

## 3) Use `BasePage`

```xaml
<pages:BasePage x:Class="YourApp.Views.DemoPage"
                xmlns:pages="clr-namespace:Stackdose.UI.Templates.Pages;assembly=Stackdose.UI.Templates"
                PageTitle="Demo"
                LogoutRequested="OnLogout"
                NavigationRequested="OnNavigate">
  <pages:BasePage.ContentArea>
    <Grid Margin="24" />
  </pages:BasePage.ContentArea>
</pages:BasePage>
```

## 4) Handle forwarded events in app layer

- `LogoutRequested`
- `SwitchUserRequested`
- `NavigationRequested`
- `MinimizeRequested`
- `CloseRequested`
