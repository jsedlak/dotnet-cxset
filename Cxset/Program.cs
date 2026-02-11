using System.CommandLine;
using Cxset.Commands;

var rootCommand = new RootCommand("Manage versions and changelogs for .NET projects");

rootCommand.Add(new AddCommand());
rootCommand.Add(new ExplainCommand());
rootCommand.Add(new PublishCommand());
rootCommand.Add(new ValidateCommand());
rootCommand.Add(new VersionCommand());

return rootCommand.Parse(args).Invoke();
