namespace WTDR.Next;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Microsoft.UI.Xaml.Window? _window;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var deployMode = Environment.GetCommandLineArgs()
            .Any(value => value.Equals("/Deploy", StringComparison.OrdinalIgnoreCase) || value.Equals("--deploy", StringComparison.OrdinalIgnoreCase));
        var desktopMode = Environment.GetCommandLineArgs()
            .Any(value => value.Equals("/FirstDesktop", StringComparison.OrdinalIgnoreCase) || value.Equals("--first-desktop", StringComparison.OrdinalIgnoreCase));
        _window = deployMode ? new DeploymentWindow("Deploy") : desktopMode ? new DeploymentWindow("Desktop") : new MainWindow();
        _window.Activate();
    }
}
