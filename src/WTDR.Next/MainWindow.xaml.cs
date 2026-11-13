using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using WTDR.Next.Models;
using WTDR.Next.Services;

namespace WTDR.Next;

public partial class MainWindow : Window
{
    private readonly PackService _packService = new();
    private readonly string _packDirectory = Path.Combine(AppContext.BaseDirectory, "Pack");
    private IReadOnlyList<PackInfo> _allPacks = [];

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        SetWindowTitle("WTDR.Next - Windows 部署控制台");
        EnvironmentText.Text = $"{Environment.OSVersion.VersionString}\n架构：{(Environment.Is64BitOperatingSystem ? "X64" : "X86")}\n权限：{(new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? "管理员" : "普通用户")}";
        RefreshPackages();
        Log("WTDR.Next 已启动");
    }

    private void RefreshPackages()
    {
        var packs = _packService.Scan(_packDirectory);
        _allPacks = packs;
        ApplyPackageFilter();
        PackageCountText.Text = packs.Count.ToString();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPackageFilter();
    private void ApplyPackageFilter()
    {
        var query = SearchBox?.Text?.Trim() ?? "";
        PackageList.ItemsSource = string.IsNullOrWhiteSpace(query) ? _allPacks : _allPacks.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (PackageList.SelectedItem is not PackInfo pack) { await ShowMessageAsync("请先选择一个软件包。", "WTDR.Next"); return; }
        StatusText.Text = "执行中";
        try
        {
            var progress = new Progress<string>(Log);
            await _packService.ApplyAsync(pack, "Desktop", progress, CancellationToken.None);
            StatusText.Text = "已完成";
        }
        catch (Exception ex) { StatusText.Text = "失败"; Log($"错误：{ex.Message}"); await ShowMessageAsync(ex.Message, "应用失败"); }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e) { Directory.CreateDirectory(_packDirectory); Process.Start(new ProcessStartInfo("explorer.exe", _packDirectory) { UseShellExecute = true }); }
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshPackages();
    private void Packages_Click(object sender, RoutedEventArgs e) => PackageList.Focus(FocusState.Programmatic);
    private void Logs_Click(object sender, RoutedEventArgs e) => LogBox.Focus(FocusState.Programmatic);
    private void Settings_Click(object sender, RoutedEventArgs e) => new ConfigEditorWindow().Activate();
    private void Log(string message) => LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
    private void ClearLog_Click(object sender, RoutedEventArgs e) => LogBox.Text = "";

    private void SetWindowTitle(string title)
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(this));
        AppWindow.GetFromWindowId(windowId).Title = title;
    }

    private async Task ShowMessageAsync(string message, string title)
    {
        var dialog = new ContentDialog { Title = title, Content = message, CloseButtonText = "确定", XamlRoot = (Content as FrameworkElement)?.XamlRoot };
        await dialog.ShowAsync();
    }
}
