using System.Windows;
using System.Windows.Markup;

// ?? 定義統一的 XML 命名空間（簡化 XAML 引用）
// 使用方式：xmlns:Templates="http://schemas.stackdose.com/templates"

[assembly: XmlnsDefinition("http://schemas.stackdose.com/templates", "Stackdose.UI.Templates.Shell")]
[assembly: XmlnsDefinition("http://schemas.stackdose.com/templates", "Stackdose.UI.Templates.Pages")]
[assembly: XmlnsDefinition("http://schemas.stackdose.com/templates", "Stackdose.UI.Templates.Controls")]
[assembly: XmlnsDefinition("http://schemas.stackdose.com/templates", "Stackdose.UI.Templates.Converters")]

// ?? 主題資源配置
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            // 主題特定資源的位置
    ResourceDictionaryLocation.SourceAssembly   // 通用資源字典的位置
)]
