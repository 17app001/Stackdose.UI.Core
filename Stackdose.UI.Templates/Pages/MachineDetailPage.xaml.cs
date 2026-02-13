using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Pages;

public partial class MachineDetailPage : UserControl
{
    public static readonly DependencyProperty MachineTitleProperty =
        DependencyProperty.Register(nameof(MachineTitle), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("Machine Detail"));

    public static readonly DependencyProperty BatchNumberProperty =
        DependencyProperty.Register(nameof(BatchNumber), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("B-20260213-01"));

    public static readonly DependencyProperty RecipeNameProperty =
        DependencyProperty.Register(nameof(RecipeName), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("Recipe-A01"));

    public static readonly DependencyProperty MachineStateProperty =
        DependencyProperty.Register(nameof(MachineState), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("Running"));

    public static readonly DependencyProperty AlarmStateProperty =
        DependencyProperty.Register(nameof(AlarmState), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("Normal"));

    public string MachineTitle
    {
        get => (string)GetValue(MachineTitleProperty);
        set => SetValue(MachineTitleProperty, value);
    }

    public string BatchNumber
    {
        get => (string)GetValue(BatchNumberProperty);
        set => SetValue(BatchNumberProperty, value);
    }

    public string RecipeName
    {
        get => (string)GetValue(RecipeNameProperty);
        set => SetValue(RecipeNameProperty, value);
    }

    public string MachineState
    {
        get => (string)GetValue(MachineStateProperty);
        set => SetValue(MachineStateProperty, value);
    }

    public string AlarmState
    {
        get => (string)GetValue(AlarmStateProperty);
        set => SetValue(AlarmStateProperty, value);
    }

    public MachineDetailPage()
    {
        InitializeComponent();
    }
}
