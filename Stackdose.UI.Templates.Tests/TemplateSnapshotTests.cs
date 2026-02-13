using System.Text.RegularExpressions;
using Xunit;

namespace Stackdose.UI.Templates.Tests;

public class TemplateSnapshotTests
{
    [Fact]
    public void CommonColors_ContainsTemplateDesignTokens()
    {
        var content = ReadProjectFile("Stackdose.UI.Templates/Resources/CommonColors.xaml");

        Assert.Contains("Template.PagePadding", content);
        Assert.Contains("Template.PanelCard", content);
        Assert.Contains("Template.HeaderActionButton", content);
        Assert.Contains("Template.HeaderWindowButton", content);
        Assert.Contains("Template.SectionTitleText", content);
    }

    [Fact]
    public void AppHeader_UsesSharedButtonStyles()
    {
        var content = ReadProjectFile("Stackdose.UI.Templates/Controls/AppHeader.xaml");

        Assert.Contains("Style=\"{StaticResource Template.HeaderActionButton}\"", content);
        Assert.Contains("Style=\"{StaticResource Template.HeaderWindowButton}\"", content);
        Assert.DoesNotContain("<Button.Style>", content);
    }

    [Fact]
    public void MachineTemplatePages_ContainExpectedSections()
    {
        var overview = ReadProjectFile("Stackdose.UI.Templates/Pages/MachineOverviewPage.xaml");
        var detail = ReadProjectFile("Stackdose.UI.Templates/Pages/MachineDetailPage.xaml");

        Assert.Contains("Machine Overview", overview);
        Assert.Contains("MachineCard", overview);
        Assert.Contains("Command Panel", detail);
        Assert.Contains("Live Status", detail);

        var normalized = Regex.Replace(overview + detail, "\\s+", " ");
        Assert.Contains("Template.PanelCard", normalized);
    }

    private static string ReadProjectFile(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null && !File.Exists(Path.Combine(current.FullName, "Stackdose.UI.Core.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);

        var fullPath = Path.Combine(current!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath), $"Missing file: {fullPath}");
        return File.ReadAllText(fullPath);
    }
}
