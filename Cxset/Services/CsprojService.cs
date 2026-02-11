using System.Text.RegularExpressions;

namespace Cxset.Services;

public partial class CsprojService
{
    public List<string> FindAllCsprojFiles(string? searchPath = null)
    {
        searchPath ??= Directory.GetCurrentDirectory();
        return Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories).ToList();
    }

    public List<string> FindEligibleCsprojFiles(string? searchPath = null)
    {
        var allFiles = FindAllCsprojFiles(searchPath);
        return allFiles.Where(file => VersionElementRegex().IsMatch(File.ReadAllText(file))).ToList();
    }

    public bool HasVersionElement(string filePath)
    {
        var content = File.ReadAllText(filePath);
        return VersionElementRegex().IsMatch(content);
    }

    public bool IsPackable(string filePath)
    {
        var content = File.ReadAllText(filePath);
        if (PackableRegex().IsMatch(content))
            return true;

        // Check Directory.Build.props files walking up from the project directory
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        while (dir != null)
        {
            var propsFile = Path.Combine(dir, "Directory.Build.props");
            if (File.Exists(propsFile))
            {
                var propsContent = File.ReadAllText(propsFile);
                if (PackableRegex().IsMatch(propsContent))
                    return true;
            }
            dir = Path.GetDirectoryName(dir);
        }

        return false;
    }

    public string? GetVersion(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var match = VersionValueRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    public void UpdateVersion(string filePath, string newVersion)
    {
        var content = File.ReadAllText(filePath);
        var updatedContent = VersionElementRegex().Replace(content, $"<Version>{newVersion}</Version>");
        File.WriteAllText(filePath, updatedContent);
    }

    [GeneratedRegex(@"<Version>.*?</Version>")]
    private static partial Regex VersionElementRegex();

    [GeneratedRegex(@"<Version>(.*?)</Version>")]
    private static partial Regex VersionValueRegex();

    [GeneratedRegex(@"<(IsPackable|PackAsTool)>\s*true\s*</(IsPackable|PackAsTool)>", RegexOptions.IgnoreCase)]
    private static partial Regex PackableRegex();
}
