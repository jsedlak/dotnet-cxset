using System.Text;

namespace Cxset.Services;

public class ChangelogService
{
    private const string ChangelogFileName = "CHANGELOG.md";

    public void AppendToChangelog(string version, string content, string? directory = null)
    {
        var changelogPath = directory != null
            ? Path.Combine(directory, ChangelogFileName)
            : ChangelogFileName;

        var sb = new StringBuilder();

        if (File.Exists(changelogPath))
        {
            sb.Append(File.ReadAllText(changelogPath));
            if (!sb.ToString().EndsWith('\n'))
            {
                sb.AppendLine();
            }
            sb.AppendLine();
        }

        sb.AppendLine($"## {version}");
        sb.AppendLine();
        sb.AppendLine(content.TrimEnd());

        File.WriteAllText(changelogPath, sb.ToString());
    }
}
