using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives; // ?? 加入這個引用
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// CyberTabControl - A TabControl with Cyber theme styling
    /// All areas are dark themed including content area
    /// </summary>
    public class CyberTabControl : TabControl
    {
        static CyberTabControl()
        {
            // Override the default style to use our custom template
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CyberTabControl),
                new FrameworkPropertyMetadata(typeof(CyberTabControl)));
        }

        public CyberTabControl()
        {
            // Force dark background for the entire control
            var darkBg = Brushes.Black;
            this.Background = darkBg;
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x5F, 0x7F));
            this.BorderThickness = new Thickness(1);
            
            this.Loaded += CyberTabControl_Loaded;
            
            // Apply custom template immediately
            ApplyCustomTemplate();
        }

        private void ApplyCustomTemplate()
        {
            // Create custom ControlTemplate for TabControl
            var template = new ControlTemplate(typeof(TabControl));
            
            // Create Grid as root
            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.SetValue(Panel.BackgroundProperty, Brushes.Black);
            
            // Add Row Definitions
            gridFactory.AppendChild(CreateRowDefinition(GridLength.Auto));
            gridFactory.AppendChild(CreateRowDefinition(new GridLength(1, GridUnitType.Star)));
            
            // Tab Header Border
            var headerBorderFactory = new FrameworkElementFactory(typeof(Border));
            headerBorderFactory.SetValue(Grid.RowProperty, 0);
            headerBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            headerBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x4A, 0x5F, 0x7F)));
            headerBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 1));
            headerBorderFactory.SetValue(Border.PaddingProperty, new Thickness(4, 4, 4, 0));
            
            // TabPanel for headers
            var tabPanelFactory = new FrameworkElementFactory(typeof(TabPanel));
            tabPanelFactory.SetValue(Panel.IsItemsHostProperty, true);
            tabPanelFactory.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
            headerBorderFactory.AppendChild(tabPanelFactory);
            
            gridFactory.AppendChild(headerBorderFactory);
            
            // Content Border - THIS IS THE KEY PART
            var contentBorderFactory = new FrameworkElementFactory(typeof(Border));
            contentBorderFactory.SetValue(Grid.RowProperty, 1);
            contentBorderFactory.SetValue(Border.BackgroundProperty, Brushes.Black); // DARK BACKGROUND
            contentBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            contentBorderFactory.SetValue(Border.PaddingProperty, new Thickness(8));
            
            // ContentPresenter
            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.ContentSourceProperty, "SelectedContent");
            contentBorderFactory.AppendChild(contentPresenterFactory);
            
            gridFactory.AppendChild(contentBorderFactory);
            
            template.VisualTree = gridFactory;
            this.Template = template;
        }

        private FrameworkElementFactory CreateRowDefinition(GridLength height)
        {
            var rowDefFactory = new FrameworkElementFactory(typeof(RowDefinition));
            rowDefFactory.SetValue(RowDefinition.HeightProperty, height);
            return rowDefFactory;
        }

        private void CyberTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply TabItem style
            if (this.ItemContainerStyle == null)
            {
                var style = CreateCyberTabItemStyle();
                this.ItemContainerStyle = style;
            }
        }

        private Style CreateCyberTabItemStyle()
        {
            var style = new Style(typeof(TabItem));
            
            // Background - transparent for unselected
            style.Setters.Add(new Setter(TabItem.BackgroundProperty, Brushes.Transparent));
            
            // Foreground - Dark gray for unselected tabs
            var unselectedBrush = new SolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70));
            style.Setters.Add(new Setter(TabItem.ForegroundProperty, unselectedBrush));
            
            // Padding
            style.Setters.Add(new Setter(TabItem.PaddingProperty, new Thickness(16, 8, 16, 8)));
            
            // Margin
            style.Setters.Add(new Setter(TabItem.MarginProperty, new Thickness(0, 0, 2, 0)));
            
            // Font
            style.Setters.Add(new Setter(TabItem.FontSizeProperty, 14.0));
            
            // Selected state trigger
            var selectedTrigger = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            
            // Selected: Neon Green
            var neonGreen = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x7F));
            selectedTrigger.Setters.Add(new Setter(TabItem.ForegroundProperty, neonGreen));
            selectedTrigger.Setters.Add(new Setter(TabItem.FontWeightProperty, FontWeights.SemiBold));
            
            // Selected: darker background
            var selectedBg = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)); // Very dark gray for selected tab to distinguish from black bg
            selectedTrigger.Setters.Add(new Setter(TabItem.BackgroundProperty, selectedBg));
            
            style.Triggers.Add(selectedTrigger);
            
            return style;
        }
    }
}
