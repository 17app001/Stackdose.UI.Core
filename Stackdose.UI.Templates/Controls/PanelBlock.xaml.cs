using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Controls;

public partial class PanelBlock : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PanelBlock), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BlockContentProperty =
        DependencyProperty.Register(nameof(BlockContent), typeof(object), typeof(PanelBlock), new PropertyMetadata(null));

    public static readonly DependencyProperty BlockPaddingProperty =
        DependencyProperty.Register(nameof(BlockPadding), typeof(Thickness), typeof(PanelBlock), new PropertyMetadata(new Thickness(16)));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? BlockContent
    {
        get => GetValue(BlockContentProperty);
        set => SetValue(BlockContentProperty, value);
    }

    public Thickness BlockPadding
    {
        get => (Thickness)GetValue(BlockPaddingProperty);
        set => SetValue(BlockPaddingProperty, value);
    }

    public PanelBlock()
    {
        InitializeComponent();
    }
}
