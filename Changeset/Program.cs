using System.CommandLine;
using Changeset.Commands;

var rootCommand = new RootCommand("Manage versions and changelogs for .NET projects");

rootCommand.Add(new AddCommand());
rootCommand.Add(new PublishCommand());

return rootCommand.Parse(args).Invoke();
