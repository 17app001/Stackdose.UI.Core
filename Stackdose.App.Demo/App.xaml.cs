using Stackdose.UI.Templates.Helpers;
using System.Windows;

namespace Stackdose.App.Demo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppThemeBootstrapper.Apply(this);
            base.OnStartup(e);
        }
    }
}
