using System.Text.RegularExpressions;

namespace Cxset.Services;

public partial class CsprojService
{
    public List<string> FindEligibleCsprojFiles(string? searchPath = null)
    {
        searchPath ??= Directory.GetCurrentDirectory();
        var eligibleFiles = new List<string>();

        var csprojFiles = Directory.GetFiles(searchPath, "*.csproj", SearchOption.AllDirectories);

        foreach (var file in csprojFiles)
        {
            var content = File.ReadAllText(file);
            if (VersionElementRegex().IsMatch(content))
            {
                eligibleFiles.Add(file);
            }
        }

        return eligibleFiles;
    }

    public void UpdateVersion(string filePath, string newVersion)
    {
        var content = File.ReadAllText(filePath);
        var updatedContent = VersionElementRegex().Replace(content, $"<Version>{newVersion}</Version>");
        File.WriteAllText(filePath, updatedContent);
    }

    [GeneratedRegex(@"<Version>.*?</Version>")]
    private static partial Regex VersionElementRegex();
}
