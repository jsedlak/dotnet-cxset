using Cxset.Models;

namespace Cxset.Services;

public class VersionService
{
    private const string VersionFile = ".changes/.version";

    public string GetCurrentVersion()
    {
        if (File.Exists(VersionFile))
        {
            var content = File.ReadAllText(VersionFile).Trim();
            if (!string.IsNullOrEmpty(content))
            {
                return content;
            }
        }
        return "0.0.0";
    }

    public string BumpVersion(string version, ChangeType type)
    {
        var parts = version.Split('.');
        if (parts.Length != 3)
        {
            parts = ["0", "0", "0"];
        }

        if (!int.TryParse(parts[0], out var major)) major = 0;
        if (!int.TryParse(parts[1], out var minor)) minor = 0;
        if (!int.TryParse(parts[2], out var patch)) patch = 0;

        switch (type)
        {
            case ChangeType.Major:
                major++;
                minor = 0;
                patch = 0;
                break;
            case ChangeType.Minor:
                minor++;
                patch = 0;
                break;
            case ChangeType.Patch:
                patch++;
                break;
        }

        return $"{major}.{minor}.{patch}";
    }

    public void SaveVersion(string version)
    {
        var dir = Path.GetDirectoryName(VersionFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(VersionFile, version);
    }
}
