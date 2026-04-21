using Stackdose.App.Monitor.Services;
using Stackdose.UI.Core.Controls;
using Stackdose.UI.Core.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Stackdose.App.Monitor.ViewModels;

internal sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly SinglePageRuntimeService _runtimeService;
    private string _headerDeviceName = "SINGLE-DESIGNER";
    private string _pageTitle = "Single Detail Designer";

    public MainWindowViewModel(SinglePageRuntimeService runtimeService, Action closeAction, Action minimizeAction)
    {
        _runtimeService = runtimeService;
        CloseCommand = new RelayCommand(_ => closeAction());
        LogoutCommand = new RelayCommand(_ => closeAction());
        MinimizeCommand = new RelayCommand(_ => minimizeAction());
        SecuredSampleButtonCommand = new RelayCommand(_ => ShowSecuredSampleMessage());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CloseCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand SecuredSampleButtonCommand { get; }

    public string HeaderDeviceName
    {
        get => _headerDeviceName;
        private set
        {
            if (_headerDeviceName == value) return;
            _headerDeviceName = value;
            OnPropertyChanged();
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set
        {
            if (_pageTitle == value) return;
            _pageTitle = value;
            OnPropertyChanged();
        }
    }

    public bool TryInitialize(out SinglePageRuntimeConfig runtime)
    {
        if (!_runtimeService.TryLoad(out runtime))
        {
            return false;
        }

        HeaderDeviceName = runtime.MachineName;
        PageTitle = $"{runtime.MachineName} - Single Detail";
        return true;
    }

    public void AttachEvents()
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
        PlcEventContext.EventTriggered += OnPlcEventTriggered;
    }

    public void DetachEvents()
    {
        PlcEventContext.EventTriggered -= OnPlcEventTriggered;
    }

    private void OnPlcEventTriggered(object? sender, PlcEventTriggeredEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.EventName))
        {
            return;
        }

        HandlePlcEvent(e.EventName, e);
    }

    private void HandlePlcEvent(string eventName, PlcEventTriggeredEventArgs e)
    {
        switch (eventName.Trim().ToLowerInvariant())
        {
            case "recipestart":
                ShowEventMessage("RecipeStart", e.Address);
                break;
            default:
                ShowEventMessage(eventName, e.Address);
                break;
        }
    }

    private void ShowEventMessage(string eventName, string address)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        PageTitle = $"{HeaderDeviceName} - {eventName} @ {timestamp}";
        CyberMessageBox.Show(
            message: $"PLC Event Triggered\nName: {eventName}\nAddress: {address}\nTime: {timestamp}",
            title: "PLC Event",
            buttons: MessageBoxButton.OK,
            icon: MessageBoxImage.Information);
    }

    private void ShowSecuredSampleMessage()
    {
        CyberMessageBox.Show(
            message: "SecuredButton click event triggered via MainWindowViewModel.",
            title: "SecuredButton Sample",
            buttons: MessageBoxButton.OK,
            icon: MessageBoxImage.Information);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
