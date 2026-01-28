using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Templates.Controls
{
    public partial class MachineCard : UserControl
    {
        public MachineCard()
        {
            InitializeComponent();

            // reasonable defaults
            AccentBrush = Brushes.White;
            StatusBrush = Brushes.Gray;
            BatchLabel = "Batch No.";
        }

        // Title / Batch / Recipe / Status
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(MachineCard), new PropertyMetadata("Machine"));

        public string BatchLabel { get => (string)GetValue(BatchLabelProperty); set => SetValue(BatchLabelProperty, value); }
        public static readonly DependencyProperty BatchLabelProperty =
            DependencyProperty.Register(nameof(BatchLabel), typeof(string), typeof(MachineCard), new PropertyMetadata("Batch No."));

        public string BatchValue { get => (string)GetValue(BatchValueProperty); set => SetValue(BatchValueProperty, value); }
        public static readonly DependencyProperty BatchValueProperty =
            DependencyProperty.Register(nameof(BatchValue), typeof(string), typeof(MachineCard), new PropertyMetadata("--"));

        public string RecipeText { get => (string)GetValue(RecipeTextProperty); set => SetValue(RecipeTextProperty, value); }
        public static readonly DependencyProperty RecipeTextProperty =
            DependencyProperty.Register(nameof(RecipeText), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(MachineCard), new PropertyMetadata("Idle"));

        // Brushes
        public Brush AccentBrush { get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }
        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(MachineCard), new PropertyMetadata(Brushes.White));

        public Brush StatusBrush { get => (Brush)GetValue(StatusBrushProperty); set => SetValue(StatusBrushProperty, value); }
        public static readonly DependencyProperty StatusBrushProperty =
            DependencyProperty.Register(nameof(StatusBrush), typeof(Brush), typeof(MachineCard), new PropertyMetadata(Brushes.Gray));

        // Metrics (labels/values)
        public string LeftTopLabel { get => (string)GetValue(LeftTopLabelProperty); set => SetValue(LeftTopLabelProperty, value); }
        public static readonly DependencyProperty LeftTopLabelProperty =
            DependencyProperty.Register(nameof(LeftTopLabel), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string LeftTopValue { get => (string)GetValue(LeftTopValueProperty); set => SetValue(LeftTopValueProperty, value); }
        public static readonly DependencyProperty LeftTopValueProperty =
            DependencyProperty.Register(nameof(LeftTopValue), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string LeftBottomLabel { get => (string)GetValue(LeftBottomLabelProperty); set => SetValue(LeftBottomLabelProperty, value); }
        public static readonly DependencyProperty LeftBottomLabelProperty =
            DependencyProperty.Register(nameof(LeftBottomLabel), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string LeftBottomValue { get => (string)GetValue(LeftBottomValueProperty); set => SetValue(LeftBottomValueProperty, value); }
        public static readonly DependencyProperty LeftBottomValueProperty =
            DependencyProperty.Register(nameof(LeftBottomValue), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string RightTopLabel { get => (string)GetValue(RightTopLabelProperty); set => SetValue(RightTopLabelProperty, value); }
        public static readonly DependencyProperty RightTopLabelProperty =
            DependencyProperty.Register(nameof(RightTopLabel), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string RightTopValue { get => (string)GetValue(RightTopValueProperty); set => SetValue(RightTopValueProperty, value); }
        public static readonly DependencyProperty RightTopValueProperty =
            DependencyProperty.Register(nameof(RightTopValue), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string RightBottomLabel { get => (string)GetValue(RightBottomLabelProperty); set => SetValue(RightBottomLabelProperty, value); }
        public static readonly DependencyProperty RightBottomLabelProperty =
            DependencyProperty.Register(nameof(RightBottomLabel), typeof(string), typeof(MachineCard), new PropertyMetadata(""));

        public string RightBottomValue { get => (string)GetValue(RightBottomValueProperty); set => SetValue(RightBottomValueProperty, value); }
        public static readonly DependencyProperty RightBottomValueProperty =
            DependencyProperty.Register(nameof(RightBottomValue), typeof(string), typeof(MachineCard), new PropertyMetadata(""));
    }
}
