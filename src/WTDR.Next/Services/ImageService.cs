using System.Diagnostics;

namespace WTDR.Next.Services;

public sealed class ImageService
{
    public Task<string> GetImageInfoAsync(string imagePath, CancellationToken token) => RunDismAsync($"/Get-WimInfo /WimFile:\"{imagePath}\"", token);
    public async Task ApplyImageAsync(string imagePath, int index, string destination, IProgress<string>? progress, CancellationToken token)
    {
        Directory.CreateDirectory(destination);
        await RunDismAsync($"/Apply-Image /ImageFile:\"{imagePath}\" /Index:{index} /ApplyDir:\"{destination}\" /CheckIntegrity", token, progress);
    }
    public Task MountImageAsync(string imagePath, int index, string mountDirectory, CancellationToken token)
    {
        Directory.CreateDirectory(mountDirectory);
        return RunDismAsync($"/Mount-Image /ImageFile:\"{imagePath}\" /Index:{index} /MountDir:\"{mountDirectory}\" /ReadOnly", token);
    }
    public Task UnmountImageAsync(string mountDirectory, bool discard, CancellationToken token) => RunDismAsync($"/Unmount-Image /MountDir:\"{mountDirectory}\" /{(discard ? "Discard" : "Commit")}", token);
    private static async Task<string> RunDismAsync(string arguments, CancellationToken token, IProgress<string>? progress = null)
    {
        using var process = Process.Start(new ProcessStartInfo("dism.exe", arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("无法启动 DISM");
        var output = await process.StandardOutput.ReadToEndAsync(token);
        var error = await process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);
        progress?.Report(output);
        if (process.ExitCode != 0) throw new InvalidOperationException($"DISM 执行失败：{error}{output}");
        return output;
    }
}
