using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

namespace Stackdose.UI.Core.Controls
{
    [ContentProperty(nameof(Content))]
    public class TabViewModel : INotifyPropertyChanged
    {
        private string _header = "Tab";
        private object? _content;

        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(); }
        }

        public object? Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
