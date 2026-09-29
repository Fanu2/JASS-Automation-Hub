using System.Windows;

namespace JASS.AutomationHub;

public partial class App : Application
{
    public static string DataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JASS", "AutomationHub");

    protected override void OnStartup(StartupEventArgs e)
    {
        Directory.CreateDirectory(DataFolder);
        base.OnStartup(e);
    }
}
