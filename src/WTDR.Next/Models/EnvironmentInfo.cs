namespace WTDR.Next.Models;

public sealed record EnvironmentInfo(string OsVersion, string Architecture, string Identity, bool IsAdministrator, bool IsSystem, bool IsOobe, string SystemDrive);
