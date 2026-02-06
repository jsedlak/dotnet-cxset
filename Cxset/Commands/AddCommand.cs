using System.CommandLine;
using Cxset.Models;
using Cxset.Services;
using Spectre.Console;

namespace Cxset.Commands;

public class AddCommand : Command
{
    public AddCommand() : base("add", "Add a new changeset")
    {
        this.SetAction(Execute);
    }

    private static int Execute(ParseResult parseResult)
    {
        var changesetService = new ChangesetService();
        var csprojService = new CsprojService();

        // Find all eligible csproj files
        var csprojFiles = csprojService.FindEligibleCsprojFiles();

        if (csprojFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No eligible .csproj files found (files with <Version> element).[/]");
            return 1;
        }

        // Build choices for multi-select
        var projectChoices = csprojFiles
            .Select(f => Path.GetRelativePath(Directory.GetCurrentDirectory(), f))
            .ToList();

        // Multi-select prompt for projects
        var selectedProjects = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]projects[/] affected by this change:")
                .Required()
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more projects)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                .AddChoices(projectChoices));

        if (selectedProjects.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No projects selected. Aborting.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"\nSelected [green]{selectedProjects.Count}[/] project(s):");
        foreach (var project in selectedProjects)
        {
            AnsiConsole.MarkupLine($"  [blue]{project}[/]");
        }

        // Selection prompt for change type
        var changeType = AnsiConsole.Prompt(
            new SelectionPrompt<ChangeType>()
                .Title("\nWhat [green]type of change[/] is this?")
                .AddChoices(ChangeType.Patch, ChangeType.Minor, ChangeType.Major)
                .UseConverter(type => type switch
                {
                    ChangeType.Patch => "patch  - Bug fixes, small changes",
                    ChangeType.Minor => "minor  - New features, backwards compatible",
                    ChangeType.Major => "major  - Breaking changes",
                    _ => type.ToString()
                }));

        // Text prompt for change description
        AnsiConsole.MarkupLine("\nDescribe the changes [grey](enter an empty line to finish)[/]:");

        var lines = new List<string>();
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;
            lines.Add(line);
        }

        var content = string.Join(Environment.NewLine, lines).Trim();

        if (string.IsNullOrEmpty(content))
        {
            AnsiConsole.MarkupLine("[red]No changes provided. Aborting.[/]");
            return 1;
        }

        // Save changeset
        var filePath = changesetService.SaveChangeset(changeType, content, selectedProjects);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Changeset saved to:[/] {filePath}");
        return 0;
    }
}
