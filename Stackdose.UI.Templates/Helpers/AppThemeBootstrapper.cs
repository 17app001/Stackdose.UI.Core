using System;
using System.Windows;

namespace Stackdose.UI.Templates.Helpers;

public static class AppThemeBootstrapper
{
    private const string CoreThemeUri = "pack://application:,,,/Stackdose.UI.Core;component/Themes/Theme.xaml";
    private const string TemplateColorUri = "pack://application:,,,/Stackdose.UI.Templates;component/Resources/CommonColors.xaml";

    public static void Apply(Application app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        AddIfMissing(app.Resources, CoreThemeUri);
        AddIfMissing(app.Resources, TemplateColorUri);
    }

    private static void AddIfMissing(ResourceDictionary resources, string source)
    {
        foreach (var dictionary in resources.MergedDictionaries)
        {
            if (dictionary.Source?.OriginalString == source)
            {
                return;
            }
        }

        resources.MergedDictionaries.Add(
            new ResourceDictionary
            {
                Source = new Uri(source, UriKind.Absolute)
            });
    }
}
