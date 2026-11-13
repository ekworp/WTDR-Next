using System.Text.Json;

namespace WTDR.Next.Services;

public sealed record DeploymentState(string Mode, string Stage, int StageIndex, bool Completed, DateTimeOffset UpdatedAt, string? Error);

public sealed class DeploymentStateStore
{
    private readonly string _path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WTDR.Next", "deployment-state.json");

    public DeploymentState? Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<DeploymentState>(File.ReadAllText(_path)) : null; }
        catch { return null; }
    }

    public void Save(DeploymentState state)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Clear()
    {
        try { if (File.Exists(_path)) File.Delete(_path); } catch { }
    }
}
