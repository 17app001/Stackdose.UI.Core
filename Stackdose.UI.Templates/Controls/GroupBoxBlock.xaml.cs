using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Controls;

public partial class GroupBoxBlock : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(GroupBoxBlock), new PropertyMetadata("Group"));

    public static readonly DependencyProperty BadgeTextProperty =
        DependencyProperty.Register(nameof(BadgeText), typeof(string), typeof(GroupBoxBlock), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty GroupContentProperty =
        DependencyProperty.Register(nameof(GroupContent), typeof(object), typeof(GroupBoxBlock), new PropertyMetadata(null));

    public static readonly DependencyProperty GroupPaddingProperty =
        DependencyProperty.Register(nameof(GroupPadding), typeof(Thickness), typeof(GroupBoxBlock), new PropertyMetadata(new Thickness(16)));

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public object? GroupContent
    {
        get => GetValue(GroupContentProperty);
        set => SetValue(GroupContentProperty, value);
    }

    public Thickness GroupPadding
    {
        get => (Thickness)GetValue(GroupPaddingProperty);
        set => SetValue(GroupPaddingProperty, value);
    }

    public GroupBoxBlock()
    {
        InitializeComponent();
    }
}
