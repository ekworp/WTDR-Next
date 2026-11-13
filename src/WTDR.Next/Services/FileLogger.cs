namespace WTDR.Next.Services;

public sealed class FileLogger
{
    private readonly string _path;
    private readonly object _gate = new();
    public FileLogger(string? directory = null)
    {
        var folder = directory ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WTDR.Next", "Logs");
        Directory.CreateDirectory(folder);
        _path = System.IO.Path.Combine(folder, $"WTDR-{DateTime.Now:yyyyMMdd}.log");
    }
    public string Path => _path;
    public void Info(string message) => Write("INFO", message);
    public void Error(string message) => Write("ERROR", message);
    private void Write(string level, string message) { lock (_gate) File.AppendAllText(_path, $"[{DateTime.Now:O}] [{level}] {message}{Environment.NewLine}"); }
}
