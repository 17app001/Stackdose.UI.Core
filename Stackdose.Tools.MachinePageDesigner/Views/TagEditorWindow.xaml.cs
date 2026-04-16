using System.Collections.ObjectModel;
using System.Windows;
using Stackdose.Tools.MachinePageDesigner.Models;

namespace Stackdose.Tools.MachinePageDesigner.Views;

public partial class TagEditorWindow : Window
{
    private readonly ObservableCollection<PlcTag> _tags;

    public TagEditorWindow(ObservableCollection<PlcTag> tags)
    {
        _tags = tags;
        InitializeComponent();
        TagGrid.ItemsSource = _tags;
    }

    private void OnAddRow(object sender, RoutedEventArgs e)
    {
        var tag = new PlcTag();
        _tags.Add(tag);
        TagGrid.SelectedItem = tag;
        TagGrid.ScrollIntoView(tag);
    }

    private void OnDeleteRow(object sender, RoutedEventArgs e)
    {
        if (TagGrid.SelectedItem is PlcTag tag)
            _tags.Remove(tag);
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        // 移除空白列（Address 為空視為無效）
        var blanks = _tags.Where(t => string.IsNullOrWhiteSpace(t.Address)).ToList();
        foreach (var b in blanks) _tags.Remove(b);

        DialogResult = true;
        Close();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
