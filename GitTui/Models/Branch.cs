namespace GitTui.Models;

public class Branch
{
    public required string Name { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsRemote { get; init; }
    public string? Upstream { get; init; }
}
