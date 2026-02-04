using System.CommandLine;
using System.Text;
using Cxset.Models;
using Cxset.Services;

namespace Cxset.Commands;

public class PublishCommand : Command
{
    public PublishCommand() : base("publish", "Publish all pending changesets and bump version")
    {
        this.SetAction(Execute);
    }

    private static int Execute(ParseResult parseResult)
    {
        var changesetService = new ChangesetService();
        var versionService = new VersionService();
        var csprojService = new CsprojService();
        var changelogService = new ChangelogService();

        // Read all changesets
        var changesets = changesetService.ReadAllChangesets();

        if (changesets.Count == 0)
        {
            Console.WriteLine("No changesets found in .changes/ directory.");
            return 1;
        }

        Console.WriteLine($"Found {changesets.Count} changeset(s).");

        // Determine largest change type across all changesets
        var largestType = changesets.Max(c => c.Type);
        Console.WriteLine($"Largest change type: {largestType}");

        // Get current version and bump
        var currentVersion = versionService.GetCurrentVersion();
        var newVersion = versionService.BumpVersion(currentVersion, largestType);
        Console.WriteLine($"Version: {currentVersion} -> {newVersion}");

        // Group changesets by project
        var projectChangesets = new Dictionary<string, List<ChangesetEntry>>();
        foreach (var changeset in changesets)
        {
            foreach (var project in changeset.Projects)
            {
                if (!projectChangesets.ContainsKey(project))
                {
                    projectChangesets[project] = [];
                }
                projectChangesets[project].Add(changeset);
            }
        }

        Console.WriteLine($"\nAffected projects: {projectChangesets.Count}");

        // Update csproj files and create per-project changelogs
        foreach (var (projectPath, projectChanges) in projectChangesets)
        {
            var fullPath = Path.GetFullPath(projectPath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"  Warning: Project not found: {projectPath}");
                continue;
            }

            // Update csproj version
            csprojService.UpdateVersion(fullPath, newVersion);
            Console.WriteLine($"  Updated: {projectPath}");

            // Combine changes for this project
            var projectContent = new StringBuilder();
            foreach (var change in projectChanges.OrderBy(c => c.Timestamp))
            {
                if (projectContent.Length > 0)
                {
                    projectContent.AppendLine();
                }
                projectContent.Append(change.Content);
            }

            // Write changelog to project directory
            var projectDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(projectDir))
            {
                changelogService.AppendToChangelog(newVersion, projectContent.ToString(), projectDir);
                Console.WriteLine($"  Updated CHANGELOG: {Path.Combine(projectDir, "CHANGELOG.md")}");
            }
        }

        // Save new version
        versionService.SaveVersion(newVersion);
        Console.WriteLine($"\nSaved version {newVersion} to .changes/.version");

        // Delete processed changesets
        changesetService.DeleteChangesets(changesets);
        Console.WriteLine($"Deleted {changesets.Count} processed changeset file(s).");

        Console.WriteLine($"\nSuccessfully published version {newVersion}!");
        return 0;
    }
}
