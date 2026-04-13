using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.App.DeviceFramework.Controls;

/// <summary>
/// 通用 PLC 資料格狀面板：標題 + 捲動式 2 欄 UniformGrid，每格顯示標籤名稱與 PlcLabel 數值。
/// 供 LiveData 與 DeviceStatus 兩種情境共用，透過 DependencyProperty 控制標題、資料來源與字型大小。
/// </summary>
public partial class PlcDataGridPanel : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PlcDataGridPanel),
            new PropertyMetadata("Data"));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(PlcDataGridPanel),
            new PropertyMetadata(null));

    public static readonly DependencyProperty LabelFontSizeProperty =
        DependencyProperty.Register(nameof(LabelFontSize), typeof(double), typeof(PlcDataGridPanel),
            new PropertyMetadata(11.0));

    public static readonly DependencyProperty ValueFontSizeProperty =
        DependencyProperty.Register(nameof(ValueFontSize), typeof(double), typeof(PlcDataGridPanel),
            new PropertyMetadata(18.0));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public double ValueFontSize
    {
        get => (double)GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    public PlcDataGridPanel()
    {
        InitializeComponent();
    }
}
