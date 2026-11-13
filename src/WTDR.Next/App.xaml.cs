namespace WTDR.Next;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Microsoft.UI.Xaml.Window? _window;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
