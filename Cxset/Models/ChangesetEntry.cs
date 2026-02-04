namespace Cxset.Models;

public class ChangesetEntry
{
    public required string FilePath { get; init; }
    public required ChangeType Type { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Content { get; init; }
    public required List<string> Projects { get; init; }
}
