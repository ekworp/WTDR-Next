using Microsoft.UI.Xaml;

namespace WTDR.Next;

public partial class ConfigEditorWindow : Window
{
    private readonly string _path;
    public ConfigEditorWindow()
    {
        InitializeComponent();
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
