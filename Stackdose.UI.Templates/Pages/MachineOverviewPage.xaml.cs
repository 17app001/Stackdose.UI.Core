using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Templates.Pages;

public partial class MachineOverviewPage : UserControl
{
    public static readonly DependencyProperty MachineCardsProperty =
        DependencyProperty.Register(
            nameof(MachineCards),
            typeof(ObservableCollection<MachineOverviewCard>),
            typeof(MachineOverviewPage),
            new PropertyMetadata(null));

    public ObservableCollection<MachineOverviewCard> MachineCards
    {
        get => (ObservableCollection<MachineOverviewCard>)GetValue(MachineCardsProperty);
        set => SetValue(MachineCardsProperty, value);
    }

    public MachineOverviewPage()
    {
        InitializeComponent();

        MachineCards =
        [
            new MachineOverviewCard
            {
                MachineId = "M1",
                Title = "Machine 01",
                BatchValue = "B-20260213-01",
                RecipeText = "Recipe-A01",
                StatusText = "Running",
                StatusBrush = Brushes.SeaGreen,
                LeftTopLabel = "Layer",
                LeftTopValue = "126",
                LeftBottomLabel = "Nozzle",
                LeftBottomValue = "72.4 C",
                RightTopLabel = "Speed",
                RightTopValue = "38 mm/s",
                RightBottomLabel = "ETA",
                RightBottomValue = "00:14:20"
            },
            new MachineOverviewCard
            {
                MachineId = "M2",
                Title = "Machine 02",
                BatchValue = "B-20260213-02",
                RecipeText = "Recipe-B07",
                StatusText = "Idle",
                StatusBrush = Brushes.SlateGray,
                LeftTopLabel = "Layer",
                LeftTopValue = "0",
                LeftBottomLabel = "Nozzle",
                LeftBottomValue = "25.1 C",
                RightTopLabel = "Speed",
                RightTopValue = "0 mm/s",
                RightBottomLabel = "ETA",
                RightBottomValue = "--"
            }
        ];
    }
}

public sealed class MachineOverviewCard
{
    public string MachineId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string BatchValue { get; set; } = string.Empty;
    public string RecipeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public Brush StatusBrush { get; set; } = Brushes.Gray;
    public string LeftTopLabel { get; set; } = string.Empty;
    public string LeftTopValue { get; set; } = string.Empty;
    public string LeftBottomLabel { get; set; } = string.Empty;
    public string LeftBottomValue { get; set; } = string.Empty;
    public string RightTopLabel { get; set; } = string.Empty;
    public string RightTopValue { get; set; } = string.Empty;
    public string RightBottomLabel { get; set; } = string.Empty;
    public string RightBottomValue { get; set; } = string.Empty;
}
