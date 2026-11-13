namespace WTDR.Next.Models;

public sealed class WtdrConfiguration
{
    public Dictionary<string, List<TaskDefinition>> Stages { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool PackEnabled { get; init; }
    public bool HostNameEnabled { get; init; }
    public string HostNameMode { get; init; } = "";
    public string HostNamePrefix { get; init; } = "";
    public bool AdministratorEnabled { get; init; }
    public bool UacEnabled { get; init; }
    public int UacLevel { get; init; }
    public bool DriveLetterEnabled { get; init; }
    public bool OsUpdateEnabled { get; init; }
    public bool OsUpdatePackEnabled { get; init; }
    public bool DeleteDeploymentFiles { get; init; }
    public bool DisableDefender { get; init; }
    public string Wallpaper { get; init; } = "";
    public string DeploymentDirectory { get; init; } = "";
    public bool RdpEnabled { get; init; }
    public int RdpPort { get; init; }
    public bool LimitBlankPasswordRdp { get; init; }
}
