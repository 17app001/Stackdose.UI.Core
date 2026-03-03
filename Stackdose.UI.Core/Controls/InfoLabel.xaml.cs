using Stackdose.UI.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Core.Controls;

public partial class InfoLabel : UserControl
{
    public InfoLabel()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(InfoLabel), new PropertyMetadata("Label"));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(string), typeof(InfoLabel), new PropertyMetadata(string.Empty));

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty DefaultValueProperty =
        DependencyProperty.Register(nameof(DefaultValue), typeof(string), typeof(InfoLabel), new PropertyMetadata("-"));

    public string DefaultValue
    {
        get => (string)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    public static readonly DependencyProperty CaptionProperty =
        DependencyProperty.Register(nameof(Caption), typeof(string), typeof(InfoLabel), new PropertyMetadata(string.Empty));

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly DependencyProperty ShowCaptionProperty =
        DependencyProperty.Register(nameof(ShowCaption), typeof(bool), typeof(InfoLabel), new PropertyMetadata(true));

    public bool ShowCaption
    {
        get => (bool)GetValue(ShowCaptionProperty);
        set => SetValue(ShowCaptionProperty, value);
    }

    public static readonly DependencyProperty ShowFrameProperty =
        DependencyProperty.Register(nameof(ShowFrame), typeof(bool), typeof(InfoLabel), new PropertyMetadata(true));

    public bool ShowFrame
    {
        get => (bool)GetValue(ShowFrameProperty);
        set => SetValue(ShowFrameProperty, value);
    }

    public static readonly DependencyProperty FrameShapeProperty =
        DependencyProperty.Register(nameof(FrameShape), typeof(PlcLabelFrameShape), typeof(InfoLabel), new PropertyMetadata(PlcLabelFrameShape.Rectangle));

    public PlcLabelFrameShape FrameShape
    {
        get => (PlcLabelFrameShape)GetValue(FrameShapeProperty);
        set => SetValue(FrameShapeProperty, value);
    }

    public static readonly DependencyProperty FrameBackgroundProperty =
        DependencyProperty.Register(nameof(FrameBackground), typeof(PlcLabelColorTheme), typeof(InfoLabel), new PropertyMetadata(PlcLabelColorTheme.DarkBlue));

    public PlcLabelColorTheme FrameBackground
    {
        get => (PlcLabelColorTheme)GetValue(FrameBackgroundProperty);
        set => SetValue(FrameBackgroundProperty, value);
    }

    public static readonly DependencyProperty LabelForegroundProperty =
        DependencyProperty.Register(nameof(LabelForeground), typeof(PlcLabelColorTheme), typeof(InfoLabel), new PropertyMetadata(PlcLabelColorTheme.Default));

    public PlcLabelColorTheme LabelForeground
    {
        get => (PlcLabelColorTheme)GetValue(LabelForegroundProperty);
        set => SetValue(LabelForegroundProperty, value);
    }

    public static readonly DependencyProperty ValueForegroundProperty =
        DependencyProperty.Register(nameof(ValueForeground), typeof(PlcLabelColorTheme), typeof(InfoLabel), new PropertyMetadata(PlcLabelColorTheme.NeonBlue));

    public PlcLabelColorTheme ValueForeground
    {
        get => (PlcLabelColorTheme)GetValue(ValueForegroundProperty);
        set => SetValue(ValueForegroundProperty, value);
    }

    public static readonly DependencyProperty CaptionForegroundProperty =
        DependencyProperty.Register(nameof(CaptionForeground), typeof(PlcLabelColorTheme), typeof(InfoLabel), new PropertyMetadata(PlcLabelColorTheme.Default));

    public PlcLabelColorTheme CaptionForeground
    {
        get => (PlcLabelColorTheme)GetValue(CaptionForegroundProperty);
        set => SetValue(CaptionForegroundProperty, value);
    }

    public static readonly DependencyProperty LabelFontSizeProperty =
        DependencyProperty.Register(nameof(LabelFontSize), typeof(double), typeof(InfoLabel), new PropertyMetadata(12.0));

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public static readonly DependencyProperty ValueFontSizeProperty =
        DependencyProperty.Register(nameof(ValueFontSize), typeof(double), typeof(InfoLabel), new PropertyMetadata(20.0));

    public double ValueFontSize
    {
        get => (double)GetValue(ValueFontSizeProperty);
        set => SetValue(ValueFontSizeProperty, value);
    }

    public static readonly DependencyProperty CaptionFontSizeProperty =
        DependencyProperty.Register(nameof(CaptionFontSize), typeof(double), typeof(InfoLabel), new PropertyMetadata(9.0));

    public double CaptionFontSize
    {
        get => (double)GetValue(CaptionFontSizeProperty);
        set => SetValue(CaptionFontSizeProperty, value);
    }

    public static readonly DependencyProperty LabelAlignmentProperty =
        DependencyProperty.Register(nameof(LabelAlignment), typeof(HorizontalAlignment), typeof(InfoLabel), new PropertyMetadata(HorizontalAlignment.Left));

    public HorizontalAlignment LabelAlignment
    {
        get => (HorizontalAlignment)GetValue(LabelAlignmentProperty);
        set => SetValue(LabelAlignmentProperty, value);
    }

    public static readonly DependencyProperty ValueAlignmentProperty =
        DependencyProperty.Register(nameof(ValueAlignment), typeof(HorizontalAlignment), typeof(InfoLabel), new PropertyMetadata(HorizontalAlignment.Right));

    public HorizontalAlignment ValueAlignment
    {
        get => (HorizontalAlignment)GetValue(ValueAlignmentProperty);
        set => SetValue(ValueAlignmentProperty, value);
    }

    public static readonly DependencyProperty CaptionAlignmentProperty =
        DependencyProperty.Register(nameof(CaptionAlignment), typeof(HorizontalAlignment), typeof(InfoLabel), new PropertyMetadata(HorizontalAlignment.Left));

    public HorizontalAlignment CaptionAlignment
    {
        get => (HorizontalAlignment)GetValue(CaptionAlignmentProperty);
        set => SetValue(CaptionAlignmentProperty, value);
    }
}
