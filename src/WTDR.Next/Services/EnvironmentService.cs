using System.Security.Principal;
using WTDR.Next.Models;

namespace WTDR.Next.Services;

public sealed class EnvironmentService
{
    public EnvironmentInfo Inspect()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        var name = identity.Name;
        return new EnvironmentInfo(Environment.OSVersion.VersionString, Environment.Is64BitOperatingSystem ? "X64" : "X86", name,
            principal.IsInRole(WindowsBuiltInRole.Administrator), name.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase),
            string.Equals(Environment.GetEnvironmentVariable("UserName"), "defaultuser0", StringComparison.OrdinalIgnoreCase),
            Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
    }
}
