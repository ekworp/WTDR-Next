using Microsoft.UI.Xaml;
using WTDR.Next.Models;
using WTDR.Next.Services;

namespace WTDR.Next;

public partial class DeploymentWindow : Window
{
    private readonly WtdrConfigurationLoader _loader = new();
    private readonly TaskEngine _engine = new();
    private readonly PackService _packService = new();
    private readonly string _mode;

    public DeploymentWindow(string mode)
    {
        _mode = mode;
        InitializeComponent();
        _ = RunDeploymentAsync();
    }

    private async Task RunDeploymentAsync()
    {
        try
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "WTDR_Config.ini");
            if (!File.Exists(configPath))
            {
                Log($"未找到配置文件：{configPath}");
                StageText.Text = "等待部署配置";
                CurrentTaskText.Text = "请将 WTDR_Config.ini 放置到程序目录。";
                TaskProgress.IsIndeterminate = false;
                TaskProgress.Value = 0;
                return;
            }

            var configuration = _loader.Load(configPath);
            var stages = _mode == "Desktop" ? new[] { "FirstLogon", "FirstDesktop" } : new[] { "ReadyDeploy", "Deploying", "DeployOK" };
            var progress = new Progress<string>(message =>
            {
                CurrentTaskText.Text = message;
                Log(message);
            });
            for (var index = 0; index < stages.Length; index++)
            {
                StageText.Text = StageTitle(stages[index]);
                CountText.Text = $"阶段 {index + 1} / {stages.Length}";
                await _engine.RunStageAsync(configuration, stages[index], progress, CancellationToken.None);
            }

            var packConfigPath = Path.Combine(AppContext.BaseDirectory, "WTDR.Pack", "Pack_Config.ini");
            var packFolder = Path.Combine(AppContext.BaseDirectory, "WTDR.Pack", "Pack");
            if (configuration.PackEnabled && File.Exists(packConfigPath) && Directory.Exists(packFolder))
            {
                StageText.Text = "正在应用软件包";
                await _packService.ApplyConfiguredAsync(packConfigPath, packFolder, _mode, progress, CancellationToken.None);
            }

            StageText.Text = "部署完成";
            CurrentTaskText.Text = "所有部署任务已执行完成。";
            StatusText.Text = "可以关闭窗口或继续进入系统";
            TaskProgress.IsIndeterminate = false;
            TaskProgress.Value = 100;
        }
        catch (Exception ex)
        {
            StageText.Text = "部署失败";
            CurrentTaskText.Text = ex.Message;
            StatusText.Text = "请查看日志并处理失败任务";
            TaskProgress.IsIndeterminate = false;
            TaskProgress.Value = 0;
            Log($"错误：{ex}");
        }
    }

    private static string StageTitle(string stage) => stage switch
    {
        "ReadyDeploy" => "准备部署环境",
        "Deploying" => "正在部署系统",
        "DeployOK" => "完成部署收尾",
        _ => stage
    };

    private void Log(string message) => LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
}
