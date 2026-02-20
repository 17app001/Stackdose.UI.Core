namespace Stackdose.App.UbiDemo.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private string _machineConfigPath = @"Config\MachineA.config.json";
    private string _recipePath = @"Resources\recipes";
    private string _imagePath = @"Resources\images";
    private string _wavePath = @"Resources\waves";

    public string MachineConfigPath
    {
        get => _machineConfigPath;
        set => SetProperty(ref _machineConfigPath, value);
    }

    public string RecipePath
    {
        get => _recipePath;
        set => SetProperty(ref _recipePath, value);
    }

    public string ImagePath
    {
        get => _imagePath;
        set => SetProperty(ref _imagePath, value);
    }

    public string WavePath
    {
        get => _wavePath;
        set => SetProperty(ref _wavePath, value);
    }
}
