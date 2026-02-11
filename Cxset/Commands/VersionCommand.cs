using System.CommandLine;
using System.Reflection;
using Spectre.Console;

namespace Cxset.Commands;

public class VersionCommand : Command
{
    public VersionCommand() : base("version", "Display the current tool version")
    {
        this.SetAction(Execute);
    }

    private static int Execute(ParseResult parseResult)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        AnsiConsole.MarkupLine($"cxset [green]{version}[/]");
        return 0;
    }
}
