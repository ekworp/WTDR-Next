namespace WTDR.Next.Models;

public sealed class PackConfig
{
    public string Title { get; set; } = "未命名软件包";
    public string ApplyPath { get; set; } = "";
    public string RunTiming { get; set; } = "";
    public string System { get; set; } = "";
    public string Architecture { get; set; } = "";
    public List<string> BeforeTasks { get; } = [];
    public List<string> AfterTasks { get; } = [];
}
