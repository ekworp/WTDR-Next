namespace WTDR.Next.Services;

internal sealed class IniFile
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public static IniFile Load(string path)
    {
        var ini = new IniFile();
        string section = "";
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                ini._sections.TryAdd(section, new(StringComparer.OrdinalIgnoreCase));
                continue;
            }
            var split = line.IndexOf('=');
            if (split > 0) ini._sections.GetValueOrDefault(section)?.TryAdd(line[..split].Trim(), line[(split + 1)..].Trim());
        }
        return ini;
    }

    public string Get(string section, string key, string fallback = "") =>
        _sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value) ? value : fallback;

    public IEnumerable<KeyValuePair<string, string>> Values(string section) =>
        _sections.TryGetValue(section, out var values) ? values : [];
}
