namespace WTDR.Next.Models;

public sealed record TaskDefinition(
    string Kind,
    string Command,
    string Arguments = "",
    bool Hidden = false,
    bool Wait = true)
{
    public static TaskDefinition Parse(string value)
    {
        var parts = value.Split('|');
        if (parts.Length < 2) throw new FormatException($"无效任务格式：{value}");
        var kind = parts[0].Trim().ToUpperInvariant();
        var command = parts[1].Trim();
        var arguments = parts.Length > 2 ? parts[2].Trim() : "";
        var hidden = parts.Length > 3 && parts[3].Trim() == "1";
        var wait = parts.Length <= 4 || parts[4].Trim() != "0";
        return new TaskDefinition(kind, command, arguments, hidden, wait);
    }
}
