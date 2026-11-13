namespace WTDR.Next.Models;

public sealed record PackInfo(string Name, string Path, bool IsArchive)
{
    public string DisplayName => System.IO.Path.GetFileNameWithoutExtension(Name);
}
