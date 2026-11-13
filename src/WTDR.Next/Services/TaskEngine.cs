using System.Diagnostics;
using WTDR.Next.Models;

namespace WTDR.Next.Services;

public sealed class TaskEngine
{
    public async Task RunStageAsync(WtdrConfiguration configuration, string stage, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (!configuration.Stages.TryGetValue(stage, out var tasks)) return;
        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RunAsync(task, progress, cancellationToken);
        }
    }

    private static async Task RunAsync(TaskDefinition task, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var command = Environment.ExpandEnvironmentVariables(task.Command);
        var arguments = Environment.ExpandEnvironmentVariables(task.Arguments);
        progress?.Report($"执行 {task.Kind}：{command}");

        if (task.Kind == "C")
        {
            command = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = $"/d /s /c \"{task.Command} {task.Arguments}\"";
        }
        else if (task.Kind != "R")
        {
            throw new NotSupportedException($"暂不支持任务类型：{task.Kind}");
        }

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = task.Hidden,
            WorkingDirectory = AppContext.BaseDirectory
        }) ?? throw new InvalidOperationException($"无法启动任务：{command}");

        if (task.Wait)
        {
            await process.WaitForExitAsync(cancellationToken);
            progress?.Report($"任务结束，返回码：{process.ExitCode}");
            if (process.ExitCode != 0) throw new InvalidOperationException($"任务失败，返回码：{process.ExitCode}：{task.Command}");
        }
    }
}
