using System.Text;
using System.Text.RegularExpressions;
using Cxset.Models;

namespace Cxset.Services;

public partial class ChangesetService
{
    private const string ChangesDir = ".changes";

    public void EnsureChangesDirectory()
    {
        if (!Directory.Exists(ChangesDir))
        {
            Directory.CreateDirectory(ChangesDir);
        }
    }

    public string SaveChangeset(ChangeType type, string content, List<string> projects)
    {
        EnsureChangesDirectory();

        var timestamp = DateTime.UtcNow;
        var fileName = $"{timestamp:yyyyMMdd-HHmmss}.md";
        var filePath = Path.Combine(ChangesDir, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"changeset: {type.ToString().ToLowerInvariant()}");
        sb.AppendLine($"timestamp: {timestamp:O}");
        sb.AppendLine("projects:");
        foreach (var project in projects)
        {
            sb.AppendLine($"  - {project}");
        }
        sb.AppendLine("---");
        sb.AppendLine(content.TrimEnd());

        File.WriteAllText(filePath, sb.ToString());
        return filePath;
    }

    public List<ChangesetEntry> ReadAllChangesets()
    {
        var entries = new List<ChangesetEntry>();

        if (!Directory.Exists(ChangesDir))
        {
            return entries;
        }

        var files = Directory.GetFiles(ChangesDir, "*.md");

        foreach (var file in files)
        {
            var entry = ParseChangesetFile(file);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.Timestamp).ToList();
    }

    private ChangesetEntry? ParseChangesetFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var match = FrontmatterRegex().Match(content);

        if (!match.Success)
        {
            return null;
        }

        var frontmatter = match.Groups[1].Value;
        var body = content[(match.Index + match.Length)..].Trim();

        var typeMatch = ChangesetTypeRegex().Match(frontmatter);
        var timestampMatch = TimestampRegex().Match(frontmatter);

        if (!typeMatch.Success || !timestampMatch.Success)
        {
            return null;
        }

        if (!Enum.TryParse<ChangeType>(typeMatch.Groups[1].Value, ignoreCase: true, out var changeType))
        {
            return null;
        }

        if (!DateTime.TryParse(timestampMatch.Groups[1].Value, out var timestamp))
        {
            return null;
        }

        // Parse projects
        var projects = new List<string>();
        var projectMatches = ProjectItemRegex().Matches(frontmatter);
        foreach (Match projectMatch in projectMatches)
        {
            projects.Add(projectMatch.Groups[1].Value.Trim());
        }

        return new ChangesetEntry
        {
            FilePath = filePath,
            Type = changeType,
            Timestamp = timestamp,
            Content = body,
            Projects = projects
        };
    }

    public void DeleteChangesets(IEnumerable<ChangesetEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (File.Exists(entry.FilePath))
            {
                File.Delete(entry.FilePath);
            }
        }
    }

    [GeneratedRegex(@"^---\s*\n(.*?)\n---", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();

    [GeneratedRegex(@"changeset:\s*(\w+)")]
    private static partial Regex ChangesetTypeRegex();

    [GeneratedRegex(@"timestamp:\s*(.+)")]
    private static partial Regex TimestampRegex();

    [GeneratedRegex(@"^\s*-\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex ProjectItemRegex();
}
