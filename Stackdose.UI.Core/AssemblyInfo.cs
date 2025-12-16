using System.Windows;

using System.Windows.Markup;

// 定義一個統一的 XML 命名空間網址，隨便您取，通常用網址格式
[assembly: XmlnsDefinition("http://schemas.stackdose.com/wpf", "Stackdose.UI.Core.Controls")]
[assembly: XmlnsDefinition("http://schemas.stackdose.com/wpf", "Stackdose.UI.Core.Helpers")]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
