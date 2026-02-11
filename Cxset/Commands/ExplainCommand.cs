using System.CommandLine;
using Cxset.Services;
using Spectre.Console;

namespace Cxset.Commands;

public class ExplainCommand : Command
{
    public ExplainCommand() : base("explain", "Display a summary table of all discovered projects")
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

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Version")
            .AddColumn("Versioned")
            .AddColumn("Packable")
            .AddColumn("Project")
            .AddColumn("Path");

        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
            var fileName = Path.GetFileName(file);
            var dirPath = Path.GetDirectoryName(relativePath) ?? "";
            var hasVersion = csprojService.HasVersionElement(file);
            var isPackable = csprojService.IsPackable(file);
            var version = csprojService.GetVersion(file);

            var versionText = version != null
                ? $"[green]{version}[/]"
                : "[grey]-[/]";

            var versionedText = hasVersion
                ? "[green]:check_mark:[/]"
                : "[red]:cross_mark:[/]";

            var packableText = isPackable
                ? "[green]:check_mark:[/]"
                : "[red]:cross_mark:[/]";

            table.AddRow(versionText, versionedText, packableText, fileName, dirPath);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
