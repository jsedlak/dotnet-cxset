using System.CommandLine;
using System.Text;
using Cxset.Models;
using Cxset.Services;

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
            Console.WriteLine("No eligible .csproj files found (files with <Version> element).");
            return 1;
        }

        // Display projects for selection
        Console.WriteLine("Select projects affected by this change:");
        Console.WriteLine("(Enter numbers separated by commas, or 'a' for all)\n");

        for (int i = 0; i < csprojFiles.Count; i++)
        {
            var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), csprojFiles[i]);
            Console.WriteLine($"  {i + 1}. {relativePath}");
        }

        Console.Write("\nSelect projects: ");
        var projectInput = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(projectInput))
        {
            Console.WriteLine("No projects selected. Aborting.");
            return 1;
        }

        var selectedProjects = new List<string>();

        if (projectInput.Equals("a", StringComparison.OrdinalIgnoreCase) ||
            projectInput.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            selectedProjects.AddRange(csprojFiles.Select(f =>
                Path.GetRelativePath(Directory.GetCurrentDirectory(), f)));
        }
        else
        {
            var indices = projectInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var indexStr in indices)
            {
                if (int.TryParse(indexStr, out var index) && index >= 1 && index <= csprojFiles.Count)
                {
                    var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), csprojFiles[index - 1]);
                    if (!selectedProjects.Contains(relativePath))
                    {
                        selectedProjects.Add(relativePath);
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid selection: {indexStr}");
                    return 1;
                }
            }
        }

        if (selectedProjects.Count == 0)
        {
            Console.WriteLine("No valid projects selected. Aborting.");
            return 1;
        }

        Console.WriteLine($"\nSelected {selectedProjects.Count} project(s):");
        foreach (var project in selectedProjects)
        {
            Console.WriteLine($"  - {project}");
        }

        // Prompt for change type
        Console.WriteLine("\nWhat type of change is this?");
        Console.WriteLine("  1. patch - Bug fixes, small changes");
        Console.WriteLine("  2. minor - New features, backwards compatible");
        Console.WriteLine("  3. major - Breaking changes");
        Console.Write("\nSelect (1/2/3): ");

        var typeInput = Console.ReadLine()?.Trim();
        var changeType = typeInput switch
        {
            "1" or "patch" => ChangeType.Patch,
            "2" or "minor" => ChangeType.Minor,
            "3" or "major" => ChangeType.Major,
            _ => (ChangeType?)null
        };

        if (changeType == null)
        {
            Console.WriteLine("Invalid selection. Please enter 1, 2, or 3.");
            return 1;
        }

        // Prompt for change description
        Console.WriteLine("\nDescribe the changes (enter an empty line to finish):");
        var contentBuilder = new StringBuilder();
        string? line;

        while (!string.IsNullOrEmpty(line = Console.ReadLine()))
        {
            contentBuilder.AppendLine(line);
        }

        var content = contentBuilder.ToString().Trim();

        if (string.IsNullOrEmpty(content))
        {
            Console.WriteLine("No changes provided. Aborting.");
            return 1;
        }

        // Save changeset
        var filePath = changesetService.SaveChangeset(changeType.Value, content, selectedProjects);
        Console.WriteLine($"\nChangeset saved to: {filePath}");
        return 0;
    }
}
