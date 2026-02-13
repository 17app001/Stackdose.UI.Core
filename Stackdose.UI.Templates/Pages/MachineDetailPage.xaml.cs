using System.Windows;
using System.Windows.Controls;

namespace Stackdose.UI.Templates.Pages;

public partial class MachineDetailPage : UserControl
{
    public event EventHandler? StartRequested;
    public event EventHandler? StopRequested;
    public event EventHandler? ResetRequested;

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

    public static readonly DependencyProperty NozzleTempTextProperty =
        DependencyProperty.Register(nameof(NozzleTempText), typeof(string), typeof(MachineDetailPage), new PropertyMetadata("--"));

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

    public string NozzleTempText
    {
        get => (string)GetValue(NozzleTempTextProperty);
        set => SetValue(NozzleTempTextProperty, value);
    }

    public MachineDetailPage()
    {
        InitializeComponent();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }
}
