using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WTDR.Next.Models;
using WTDR.Next.Services;

namespace WTDR.Next;

public partial class MainWindow : Window
{
    private readonly PackService _packService = new();
    private readonly string _packDirectory = Path.Combine(AppContext.BaseDirectory, "Pack");

    public MainWindow()
    {
        InitializeComponent();
        EnvironmentText.Text = $"{Environment.OSVersion.VersionString}\n架构：{(Environment.Is64BitOperatingSystem ? "X64" : "X86")}\n权限：{(new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? "管理员" : "普通用户")}";
        RefreshPackages();
        Log("WTDR.Next 已启动");
    }

    private void RefreshPackages()
    {
        var packs = _packService.Scan(_packDirectory);
        PackageList.ItemsSource = packs;
        PackageCountText.Text = packs.Count.ToString();
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
    private async void Settings_Click(object sender, RoutedEventArgs e) => await ShowMessageAsync("设置中心将在后续版本提供。", "WTDR.Next");
    private void Log(string message) => LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

    private async Task ShowMessageAsync(string message, string title)
    {
        var dialog = new ContentDialog { Title = title, Content = message, CloseButtonText = "确定", XamlRoot = (Content as FrameworkElement)?.XamlRoot };
        await dialog.ShowAsync();
    }
}
