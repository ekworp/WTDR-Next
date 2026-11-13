namespace WTDR.Next.Models;

public sealed class PackConfig
{
    public string Title { get; init; } = "未命名软件包";
    public string ApplyPath { get; init; } = "";
    public string RunTiming { get; init; } = "";
    public string System { get; init; } = "";
    public string Architecture { get; init; } = "";
    public List<string> BeforeTasks { get; } = [];
    public List<string> AfterTasks { get; } = [];
}
