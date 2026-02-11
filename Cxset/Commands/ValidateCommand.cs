using System.CommandLine;
using Cxset.Services;
using Spectre.Console;

namespace Cxset.Commands;

public class ValidateCommand : Command
{
    public ValidateCommand() : base("validate", "Validate that packable projects have a Version element")
    {
        this.SetAction(Execute);
    }

    private static int Execute(ParseResult parseResult)
    {
        var csprojService = new CsprojService();
        var allFiles = csprojService.FindAllCsprojFiles();

        if (allFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No .csproj files found.[/]");
            return 0;
        }

        var hasErrors = false;

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
            var isPackable = csprojService.IsPackable(file);
            var hasVersion = csprojService.HasVersionElement(file);

            if (isPackable && !hasVersion)
            {
                AnsiConsole.MarkupLine($"  [red]:cross_mark:[/] [red]{relativePath}[/] - IsPackable but missing <Version>");
                hasErrors = true;
            }
            else if (isPackable && hasVersion)
            {
                AnsiConsole.MarkupLine($"  [green]:check_mark:[/] {relativePath}");
            }
            else
            {
                AnsiConsole.MarkupLine($"  [grey]-[/] [grey]{relativePath}[/] - Not packable");
            }
        }

        if (hasErrors)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]Validation failed. Some packable projects are missing a <Version> element.[/]");
            return 1;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]All packable projects have a <Version> element.[/]");
        return 0;
    }
}
