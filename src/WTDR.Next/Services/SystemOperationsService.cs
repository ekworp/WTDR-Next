using System.Diagnostics;
using Microsoft.Win32;
using WTDR.Next.Models;

namespace WTDR.Next.Services;

public sealed class SystemOperationsService
{
    public async Task ApplyAsync(WtdrConfiguration configuration, string root, IProgress<string>? progress, CancellationToken token)
    {
        if (configuration.HostNameEnabled)
            await SetHostNameAsync(configuration.HostNamePrefix, configuration.HostNameMode, progress, token);
        if (configuration.AdministratorEnabled)
            await RunCommandAsync("net.exe", "user Administrator /active:yes", progress, token);
        if (configuration.UacEnabled)
            await SetUacAsync(configuration.UacLevel, progress, token);
        if (configuration.DisableDefender)
            await RunPowerShellAsync("Set-MpPreference -DisableRealtimeMonitoring $true", progress, token);
        await SetRdpAsync(configuration, progress, token);
        if (!string.IsNullOrWhiteSpace(configuration.Wallpaper))
            await SetWallpaperAsync(Path.Combine(root, configuration.Wallpaper), progress, token);
        if (configuration.DeleteDeploymentFiles && !string.IsNullOrWhiteSpace(configuration.DeploymentDirectory))
            progress?.Report($"部署完成后将清理：{configuration.DeploymentDirectory}");
    }

    public async Task DeleteDeploymentDirectoryAsync(string path, IProgress<string>? progress, CancellationToken token)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (string.IsNullOrWhiteSpace(expanded) || expanded.Length < 8 || !Directory.Exists(expanded)) return;
        if (!expanded.EndsWith("Windsys_Setup\\", StringComparison.OrdinalIgnoreCase) && !expanded.EndsWith("WTDR.Next\\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"拒绝清理非部署目录：{expanded}");
        progress?.Report($"清理部署目录：{expanded}");
        await Task.Run(() => Directory.Delete(expanded, true), token);
    }

    private static async Task SetHostNameAsync(string prefix, string mode, IProgress<string>? progress, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;
        var name = BuildHostName(prefix, mode);
        progress?.Report($"设置主机名：{name}");
        await RunPowerShellAsync($"Rename-Computer -NewName '{name.Replace("'", "''")}' -Force", progress, token);
    }

    private static async Task SetUacAsync(int level, IProgress<string>? progress, CancellationToken token)
    {
        var enabled = level > 1 ? 1 : 0;
        progress?.Report($"设置 UAC 等级：{level}");
        await RunCommandAsync("reg.exe", $"ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v EnableLUA /t REG_DWORD /d {enabled} /f", progress, token);
        await RunCommandAsync("reg.exe", $"ADD HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System /v ConsentPromptBehaviorAdmin /t REG_DWORD /d {(level >= 4 ? 2 : level == 3 ? 5 : 0)} /f", progress, token);
    }

    private static async Task SetRdpAsync(WtdrConfiguration configuration, IProgress<string>? progress, CancellationToken token)
    {
        if (!configuration.RdpEnabled) return;
        progress?.Report("启用远程桌面");
        await RunCommandAsync("reg.exe", "ADD HKLM\\SYSTEM\\CurrentControlSet\\Control\\Terminal Server /v fDenyTSConnections /t REG_DWORD /d 0 /f", progress, token);
        await RunCommandAsync("netsh.exe", "advfirewall firewall set rule group=\"remote desktop\" new enable=Yes", progress, token);
        if (configuration.RdpPort is > 0 and <= 65535)
            await RunCommandAsync("netsh.exe", $"advfirewall firewall add rule name=\"WTDR.Next RDP\" dir=in action=allow protocol=TCP localport={configuration.RdpPort}", progress, token);
        if (configuration.LimitBlankPasswordRdp)
            await RunCommandAsync("reg.exe", "ADD HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa /v LimitBlankPasswordUse /t REG_DWORD /d 0 /f", progress, token);
    }

    private static async Task SetWallpaperAsync(string path, IProgress<string>? progress, CancellationToken token)
    {
        if (!File.Exists(path)) return;
        progress?.Report($"设置壁纸：{Path.GetFileName(path)}");
        await RunPowerShellAsync($"Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name Wallpaper -Value '{path.Replace("'", "''")}'", progress, token);
        await RunCommandAsync("Rundll32.exe", "USER32.DLL,UpdatePerUserSystemParameters", progress, token);
    }

    private static string BuildHostName(string prefix, string mode)
    {
        var date = DateTime.Now.ToString("yyMMdd");
        var random = Random.Shared.Next(0, 999).ToString("D3");
        var value = mode switch
        {
            "1" => prefix,
            "2" => date + random,
            _ => prefix + "-" + date + random
        };
        value = value.Replace(" ", "-");
        return value[..Math.Min(15, value.Length)];
    }

    private static async Task RunPowerShellAsync(string command, IProgress<string>? progress, CancellationToken token) =>
        await RunCommandAsync("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\"", progress, token);

    private static async Task RunCommandAsync(string fileName, string arguments, IProgress<string>? progress, CancellationToken token)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
        if (process is null) throw new InvalidOperationException($"无法启动系统命令：{fileName}");
        await process.WaitForExitAsync(token);
        if (process.ExitCode != 0) throw new InvalidOperationException($"系统命令执行失败：{fileName} {arguments}");
    }
}
