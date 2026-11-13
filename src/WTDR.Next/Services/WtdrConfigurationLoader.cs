using WTDR.Next.Models;

namespace WTDR.Next.Services;

public sealed class WtdrConfigurationLoader
{
    public WtdrConfiguration Load(string path)
    {
        var ini = IniFile.Load(path);
        var config = new WtdrConfiguration
        {
            PackEnabled = ini.Get("Pack", "Pack") == "1",
            HostNameEnabled = ini.Get("HostName", "Changer") == "1",
            HostNameMode = ini.Get("HostName", "Mode"),
            HostNamePrefix = ini.Get("HostName", "Name"),
            AdministratorEnabled = ini.Get("User", "AdminUser") == "1",
            UacEnabled = ini.Get("UAC", "UAC") == "1",
            UacLevel = int.TryParse(ini.Get("UAC", "Level"), out var level) ? level : 0,
            DriveLetterEnabled = ini.Get("DriveLetter", "Use") == "1",
            OsUpdateEnabled = ini.Get("OSUpdate", "Use") == "1",
            OsUpdatePackEnabled = ini.Get("OSUpdate", "Pack") == "1",
            DeleteDeploymentFiles = ini.Get("Other", "ByeDel_Watch") == "1"
            ,DisableDefender = ini.Get("SysOpt", "DisableWD") == "1"
            ,Wallpaper = ini.Get("SysOpt", "Wallpaper")
            ,DeploymentDirectory = ini.Get("Other", "ByeDel")
            ,RdpEnabled = ini.Get("RDP", "Enable") == "1"
            ,RdpPort = int.TryParse(ini.Get("RDP", "Port"), out var rdpPort) ? rdpPort : 0
            ,LimitBlankPasswordRdp = ini.Get("RDP", "LimitBlankPwd") == "1"
        };

        foreach (var section in new[] { "ReadyDeploy", "Deploying", "DeployOK", "FirstLogon", "FirstDesktop" })
        {
            var tasks = ini.Values(section)
                .Where(x => x.Key.StartsWith("Task", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => TaskDefinition.Parse(x.Value)).ToList();
            config.Stages[section] = tasks;
        }
        config.Stages["OSUpdate"] = ReadTasks(ini, "OSUpdate", "U_Task");
        config.Stages["OSUpdateDesktop"] = ReadTasks(ini, "OSUpdate", "D_Task");
        return config;
    }

    private static List<TaskDefinition> ReadTasks(IniFile ini, string section, string prefix) => ini.Values(section)
        .Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))
        .Select(x => TaskDefinition.Parse(x.Value)).ToList();
}
