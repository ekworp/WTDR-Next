using System.IO.Compression;
using System.Text;
using System.Diagnostics;
using WTDR.Next.Models;

namespace WTDR.Next.Services;

public sealed class PackService
{
    private static readonly string[] Extensions = [".Pack", ".7z"];

    public IReadOnlyList<PackInfo> Scan(string folder)
    {
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return Directory.EnumerateFiles(folder)
            .Where(file => Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Select(file => new PackInfo(Path.GetFileName(file), file, Path.GetExtension(file).Equals(".7z", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(pack => pack.Name).ToList();
    }

    public async Task ApplyConfiguredAsync(string configPath, string packFolder, string mode, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var ini = IniFile.Load(configPath);
        foreach (var section in ini.Sections.Where(x => x.StartsWith("Pack", StringComparison.OrdinalIgnoreCase) && x.Length > 4).OrderBy(x => x))
        {
            if (ini.Get(section, "Run") != "1") continue;
            if (mode.Equals("Update", StringComparison.OrdinalIgnoreCase) && ini.Get(section, "OSUpdate") != "1") continue;
            if (mode.Equals("Desktop", StringComparison.OrdinalIgnoreCase) && ini.Get(section, "FirstDesktop") == "0") continue;
            var name = ini.Get(section, "Pack_Name");
            if (string.IsNullOrWhiteSpace(name)) continue;
            var path = Path.Combine(packFolder, name);
            if (!File.Exists(path)) { progress?.Report($"跳过缺失软件包：{name}"); continue; }
            await ApplyAsync(new PackInfo(name, path, Path.GetExtension(path).Equals(".7z", StringComparison.OrdinalIgnoreCase)), mode, progress, cancellationToken);
        }
    }

    public async Task ApplyAsync(PackInfo pack, string mode, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var work = Path.Combine(Path.GetTempPath(), "WTDR.Next", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            progress?.Report($"准备软件包：{pack.Name}");
            if (pack.IsArchive) await Extract7zAsync(pack.Path, work, cancellationToken);
            else ZipFile.ExtractToDirectory(pack.Path, work);
            var configPath = Path.Combine(work, "@@Pack@@", "Config.ini");
            if (!File.Exists(configPath)) throw new InvalidDataException("软件包缺少 @@Pack@@\\Config.ini");
            var config = ReadConfig(configPath);
            if (!IsApplicable(config, mode)) throw new InvalidOperationException("软件包与当前运行环境不匹配");
            progress?.Report($"应用：{config.Title}");
            await RunTasksAsync(config.BeforeTasks, work, progress, cancellationToken);
            var destination = Expand(config.ApplyPath);
            if (!string.IsNullOrWhiteSpace(destination))
            {
                Directory.CreateDirectory(destination);
                CopyDirectory(work, destination, skipConfig: true);
            }
            await RunTasksAsync(config.AfterTasks, work, progress, cancellationToken);
            progress?.Report("应用完成");
        }
        finally { try { Directory.Delete(work, true); } catch { } }
    }

    private static async Task Extract7zAsync(string archive, string destination, CancellationToken token)
    {
        var sevenZip = new[] { Path.Combine(AppContext.BaseDirectory, "7z.exe"), "7z.exe", "7za.exe" }
            .FirstOrDefault(path => path.Contains(Path.DirectorySeparatorChar) ? File.Exists(path) : true);
        if (sevenZip is null) throw new FileNotFoundException("未找到 7z.exe，请将 7z.exe 放到程序目录或加入 PATH。", "7z.exe");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = sevenZip,
            Arguments = $"x -y -o\"{destination}\" \"{archive}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException("无法启动 7z.exe");
        await process.WaitForExitAsync(token);
        if (process.ExitCode != 0) throw new InvalidDataException($"7z 解压失败：{archive}");
    }

    private static PackConfig ReadConfig(string path)
    {
        var ini = IniFile.Load(path);
        var config = new PackConfig
        {
            Title = ini.Get("Pack", "Title", "未命名软件包"), ApplyPath = ini.Get("Pack", "Apply_Path"),
            RunTiming = ini.Get("Pack", "RunTiming"), System = ini.Get("Pack", "Sys"), Architecture = ini.Get("Pack", "Arch")
        };
        config.BeforeTasks.AddRange(ini.Values("Pack.Before").Where(x => x.Key.StartsWith("Task", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value).Where(x => x.Length > 0));
        config.AfterTasks.AddRange(ini.Values("Pack.After").Where(x => x.Key.StartsWith("Task", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value).Where(x => x.Length > 0));
        return config;
    }

    private static bool IsApplicable(PackConfig config, string mode) => string.IsNullOrWhiteSpace(config.RunTiming) || config.RunTiming == (mode == "Deploy" ? "1" : "2");

    private static async Task RunTasksAsync(IEnumerable<string> tasks, string root, IProgress<string>? progress, CancellationToken token)
    {
        foreach (var task in tasks)
        {
            token.ThrowIfCancellationRequested();
            var parts = task.Split('|');
            if (parts.Length < 2 || !parts[0].Equals("R", StringComparison.OrdinalIgnoreCase)) continue;
            var executable = parts[1].Contains(':') ? parts[1] : Path.Combine(root, "@@Pack@@", parts[1]);
            var args = parts.Length > 2 ? parts[2] : "";
            progress?.Report($"执行任务：{Path.GetFileName(executable)}");
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, args) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(executable) ?? root });
            if (process is not null) await process.WaitForExitAsync(token);
        }
    }

    private static string Expand(string value) => Environment.ExpandEnvironmentVariables(value.Replace("%ApplyPath%", ""));

    private static void CopyDirectory(string source, string destination, bool skipConfig)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(directory.Replace(source, destination));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (skipConfig && file.Contains(Path.Combine("@@Pack@@", "Config.ini"), StringComparison.OrdinalIgnoreCase)) continue;
            var target = file.Replace(source, destination); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target, true);
        }
    }
}
