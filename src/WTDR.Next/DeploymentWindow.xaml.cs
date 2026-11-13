using Microsoft.UI.Xaml;
using WTDR.Next.Models;
using WTDR.Next.Services;

namespace WTDR.Next;

public partial class DeploymentWindow : Window
{
    private readonly WtdrConfigurationLoader _loader = new();
    private readonly TaskEngine _engine = new();
    private readonly PackService _packService = new();
    private readonly SystemOperationsService _systemOperations = new();
    private readonly EnvironmentService _environmentService = new();
    private readonly FileLogger _logger = new();
    private readonly DeploymentStateStore _stateStore = new();
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
            var environment = _environmentService.Inspect();
            _logger.Info($"启动部署模式={_mode}, 身份={environment.Identity}, 管理员={environment.IsAdministrator}, SYSTEM={environment.IsSystem}, 架构={environment.Architecture}");
            if (!environment.IsAdministrator && !environment.IsSystem)
                throw new UnauthorizedAccessException("部署任务需要管理员或 SYSTEM 权限。");
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
            if (_mode == "Update" && !configuration.OsUpdateEnabled)
            {
                StageText.Text = "系统升级接管未启用";
                CurrentTaskText.Text = "配置中的 OSUpdate.Use 不是 1。";
                TaskProgress.IsIndeterminate = false;
                return;
            }
            var stages = _mode == "Desktop" ? new[] { "FirstLogon", "FirstDesktop" } : _mode == "Update" ? new[] { "OSUpdate", "OSUpdateDesktop" } : new[] { "ReadyDeploy", "Deploying", "DeployOK" };
            var progress = new Progress<string>(message =>
            {
                CurrentTaskText.Text = message;
                Log(message);
            });
            for (var index = 0; index < stages.Length; index++)
            {
                StageText.Text = StageTitle(stages[index]);
                CountText.Text = $"阶段 {index + 1} / {stages.Length}";
                _stateStore.Save(new DeploymentState(_mode, stages[index], index, false, DateTimeOffset.Now, null));
                await _engine.RunStageAsync(configuration, stages[index], progress, CancellationToken.None);
            }

            var packConfigPath = Path.Combine(AppContext.BaseDirectory, "WTDR.Pack", "Pack_Config.ini");
            var packFolder = Path.Combine(AppContext.BaseDirectory, "WTDR.Pack", "Pack");
            if (configuration.PackEnabled && File.Exists(packConfigPath) && Directory.Exists(packFolder))
            {
                StageText.Text = "正在应用软件包";
                await _packService.ApplyConfiguredAsync(packConfigPath, packFolder, _mode, progress, CancellationToken.None);
            }

            StageText.Text = "正在应用系统配置";
            await _systemOperations.ApplyAsync(configuration, AppContext.BaseDirectory, progress, CancellationToken.None);

            StageText.Text = "部署完成";
            CurrentTaskText.Text = "所有部署任务已执行完成。";
            StatusText.Text = "可以关闭窗口或继续进入系统";
            TaskProgress.IsIndeterminate = false;
            TaskProgress.Value = 100;
            _stateStore.Save(new DeploymentState(_mode, "Completed", stages.Length, true, DateTimeOffset.Now, null));
        }
        catch (Exception ex)
        {
            StageText.Text = "部署失败";
            CurrentTaskText.Text = ex.Message;
            StatusText.Text = "请查看日志并处理失败任务";
            TaskProgress.IsIndeterminate = false;
            TaskProgress.Value = 0;
            _stateStore.Save(new DeploymentState(_mode, StageText.Text, 0, false, DateTimeOffset.Now, ex.Message));
            _logger.Error(ex.ToString());
            Log($"错误：{ex.Message}");
        }
    }

    private static string StageTitle(string stage) => stage switch
    {
        "ReadyDeploy" => "准备部署环境",
        "Deploying" => "正在部署系统",
        "DeployOK" => "完成部署收尾",
        _ => stage
    };

    private void Log(string message) { _logger.Info(message); LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}"; }
}
