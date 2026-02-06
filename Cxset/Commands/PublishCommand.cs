using System.CommandLine;
using System.Text;
using Cxset.Models;
using Cxset.Services;
using Spectre.Console;

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
            AnsiConsole.MarkupLine("[yellow]No changesets found in .changes/ directory.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"Found [green]{changesets.Count}[/] changeset(s).");

        // Determine largest change type across all changesets
        var largestType = changesets.Max(c => c.Type);
        AnsiConsole.MarkupLine($"Largest change type: [cyan]{largestType}[/]");

        // Get current version and bump
        var currentVersion = versionService.GetCurrentVersion();
        var newVersion = versionService.BumpVersion(currentVersion, largestType);
        AnsiConsole.MarkupLine($"Version: [grey]{currentVersion}[/] → [green]{newVersion}[/]");

        // Build global changelog content from all changesets
        var globalContent = new StringBuilder();
        foreach (var changeset in changesets.OrderBy(c => c.Timestamp))
        {
            if (globalContent.Length > 0)
            {
                globalContent.AppendLine();
            }
            globalContent.AppendLine(changeset.Content);
        }

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

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Affected projects: [green]{projectChangesets.Count}[/]");

        // Create a table for project updates
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Project")
            .AddColumn("Status");

        // Update csproj files and create per-project changelogs
        foreach (var (projectPath, projectChanges) in projectChangesets)
        {
            var fullPath = Path.GetFullPath(projectPath);

            if (!File.Exists(fullPath))
            {
                table.AddRow($"[grey]{projectPath}[/]", "[red]Not found[/]");
                continue;
            }

            // Update csproj version
            csprojService.UpdateVersion(fullPath, newVersion);

            // Combine changes for this project
            var projectContent = new StringBuilder();
            foreach (var change in projectChanges.OrderBy(c => c.Timestamp))
            {
                if (projectContent.Length > 0)
                {
                    projectContent.AppendLine();
                }
                projectContent.AppendLine(change.Content);
            }

            // Write changelog to project directory
            var projectDir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(projectDir))
            {
                changelogService.AppendToChangelog(newVersion, projectContent.ToString(), projectDir);
            }

            table.AddRow($"[blue]{projectPath}[/]", "[green]Updated[/]");
        }

        AnsiConsole.Write(table);

        // Write global changelog at root level
        changelogService.AppendToChangelog(newVersion, globalContent.ToString());
        AnsiConsole.MarkupLine($"\nUpdated root [blue]CHANGELOG.md[/]");

        // Save new version
        versionService.SaveVersion(newVersion);
        AnsiConsole.MarkupLine($"Saved version [green]{newVersion}[/] to .changes/.version");

        // Delete processed changesets
        changesetService.DeleteChangesets(changesets);
        AnsiConsole.MarkupLine($"Deleted [grey]{changesets.Count}[/] processed changeset file(s).");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Successfully published version {newVersion}![/]");
        return 0;
    }
}
