using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace WTDR.Next;

public partial class ConfigEditorWindow : Window
{
    private readonly string _path;
    public ConfigEditorWindow()
    {
        InitializeComponent();
        var windowId = Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));
        AppWindow.GetFromWindowId(windowId).Title = "WTDR.Next - 配置编辑器";
        _path = Path.Combine(AppContext.BaseDirectory, "WTDR_Config.ini");
        PathText.Text = _path;
        Editor.Text = File.Exists(_path) ? File.ReadAllText(_path) : "[Standard]\r\nConfigVer=1.0\r\n\r\n[Pack]\r\nPack=1\r\n";
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, Editor.Text);
        PathText.Text = $"已保存：{_path}";
    }
}
