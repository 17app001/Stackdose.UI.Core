using Stackdose.App.SingleDetailLab.Models;
using Stackdose.App.SingleDetailLab.Services;
using Stackdose.UI.Core.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Stackdose.App.SingleDetailLab.Pages;

public partial class SingleDetailWorkspacePage : UserControl
{
    private LabMachineConfig? _config;
    private readonly List<string> _readAddresses = [];
    private int _addressIndex;
    private int _zIndex = 1;

    public SingleDetailWorkspacePage()
    {
        InitializeComponent();
    }

    public void Initialize(LabMachineConfig config)
    {
        _config = config;
        _readAddresses.Clear();
        _readAddresses.AddRange(config.Tags.Status.Values
            .Concat(config.Tags.Process.Values)
            .Where(tag => string.Equals(tag.Access, "read", StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag.Address)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase));
        _addressIndex = 0;

        MachineSummaryText.Text = $"Machine: {config.Machine.Name} ({config.Machine.Id})";
        TopPlcStatus.IpAddress = config.Plc.Ip;
        TopPlcStatus.Port = config.Plc.Port;
        TopPlcStatus.ScanInterval = config.Plc.PollIntervalMs;
        TopPlcStatus.AutoConnect = config.Plc.AutoConnect;
        TopPlcStatus.MonitorAddress = LabMonitorAddressBuilder.Build(config);

        ResetLayout();
    }

    private void ResetLayout_Click(object sender, RoutedEventArgs e)
    {
        ResetLayout();
    }

    private void AddPlcLabel_Click(object sender, RoutedEventArgs e)
    {
        var label = new PlcLabel
        {
            Label = "PLC Label",
            Address = NextAddress(),
            ShowFrame = true,
            Width = 240,
            Height = 64,
            Margin = new Thickness(0)
        };

        AddWidget("PlcLabel", label, 260, 96, 20 + _zIndex * 8, 20 + _zIndex * 8);
    }

    private void AddPlcDeviceEditor_Click(object sender, RoutedEventArgs e)
    {
        var editor = new PlcDeviceEditor
        {
            Label = "Device Editor",
            Address = NextAddress(),
            Value = "0",
            Reason = "Single page lab test",
            Width = 420,
            Height = 132,
            Margin = new Thickness(0)
        };

        AddWidget("PlcDeviceEditor", editor, 440, 168, 32 + _zIndex * 8, 32 + _zIndex * 8);
    }

    private void AddLiveLogViewer_Click(object sender, RoutedEventArgs e)
    {
        var logViewer = new LiveLogViewer
        {
            Margin = new Thickness(0)
        };

        AddWidget("LiveLogViewer", logViewer, 520, 240, 44 + _zIndex * 8, 44 + _zIndex * 8);
    }

    private void ResetLayout()
    {
        DesignerCanvas.Children.Clear();
        _zIndex = 1;

        var batch = new PlcLabel
        {
            Label = "Batch",
            Address = GetTagAddress("process", "batchNo"),
            ShowFrame = true,
            Width = 250,
            Height = 64,
            Margin = new Thickness(0)
        };
        AddWidget("Batch", batch, 270, 96, 20, 20);

        var recipe = new PlcLabel
        {
            Label = "Recipe",
            Address = GetTagAddress("process", "recipeNo"),
            ShowFrame = true,
            Width = 250,
            Height = 64,
            Margin = new Thickness(0)
        };
        AddWidget("Recipe", recipe, 270, 96, 300, 20);

        var editor = new PlcDeviceEditor
        {
            Label = "Command Editor",
            Address = GetTagAddress("process", "nozzleTemp"),
            Value = "0",
            Reason = "Single page layout test",
            Width = 430,
            Height = 132,
            Margin = new Thickness(0)
        };
        AddWidget("Editor", editor, 450, 170, 20, 140);
    }

    private void AddWidget(string title, FrameworkElement content, double width, double height, double x, double y)
    {
        var container = CreateWidgetContainer(title, content, width, height);
        container.SetValue(Canvas.LeftProperty, x);
        container.SetValue(Canvas.TopProperty, y);
        container.SetValue(Canvas.ZIndexProperty, _zIndex++);
        DesignerCanvas.Children.Add(container);
    }

    private Border CreateWidgetContainer(string title, FrameworkElement content, double width, double height)
    {
        var dragThumb = new Thumb
        {
            Cursor = System.Windows.Input.Cursors.SizeAll,
            Height = 26,
            Background = new SolidColorBrush(Color.FromRgb(35, 60, 88))
        };

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(8, 4, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false
        };

        var closeButton = new Button
        {
            Content = "X",
            Width = 24,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 3, 4, 0),
            FontSize = 11
        };

        var headerGrid = new Grid();
        headerGrid.Children.Add(dragThumb);
        headerGrid.Children.Add(titleText);
        headerGrid.Children.Add(closeButton);

        var bodyBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(20, 32, 48)),
            BorderThickness = new Thickness(1, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(56, 84, 120)),
            Child = content,
            Padding = new Thickness(8)
        };

        var layoutGrid = new Grid();
        layoutGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layoutGrid.Children.Add(headerGrid);
        Grid.SetRow(bodyBorder, 1);
        layoutGrid.Children.Add(bodyBorder);

        var container = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(56, 84, 120)),
            Background = new SolidColorBrush(Color.FromRgb(20, 32, 48)),
            Child = layoutGrid
        };

        dragThumb.DragDelta += (_, e) =>
        {
            var left = Canvas.GetLeft(container);
            var top = Canvas.GetTop(container);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;

            var nextLeft = Math.Max(0, left + e.HorizontalChange);
            var nextTop = Math.Max(0, top + e.VerticalChange);

            if (DesignerCanvas.ActualWidth > 0)
            {
                nextLeft = Math.Min(nextLeft, Math.Max(0, DesignerCanvas.ActualWidth - container.ActualWidth));
            }

            if (DesignerCanvas.ActualHeight > 0)
            {
                nextTop = Math.Min(nextTop, Math.Max(0, DesignerCanvas.ActualHeight - container.ActualHeight));
            }

            Canvas.SetLeft(container, nextLeft);
            Canvas.SetTop(container, nextTop);
        };

        container.MouseLeftButtonDown += (_, _) =>
        {
            container.SetValue(Canvas.ZIndexProperty, _zIndex++);
        };

        closeButton.Click += (_, _) =>
        {
            DesignerCanvas.Children.Remove(container);
        };

        return container;
    }

    private string NextAddress()
    {
        if (_readAddresses.Count == 0)
        {
            return "D0";
        }

        var address = _readAddresses[_addressIndex % _readAddresses.Count];
        _addressIndex++;
        return address;
    }

    private string GetTagAddress(string section, string key)
    {
        if (_config is null)
        {
            return "D0";
        }

        Dictionary<string, LabTagConfig>? tags = section.ToLowerInvariant() switch
        {
            "status" => _config.Tags.Status,
            "process" => _config.Tags.Process,
            _ => null
        };

        if (tags is null || !tags.TryGetValue(key, out var tag) || string.IsNullOrWhiteSpace(tag.Address))
        {
            return NextAddress();
        }

        return tag.Address;
    }
}
