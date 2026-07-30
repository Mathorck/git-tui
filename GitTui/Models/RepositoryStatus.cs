namespace GitTui.Models;

public class RepositoryStatus
{
    public required string BranchName { get; init; }
    public string? UpstreamName { get; init; }
    public int Ahead { get; init; }
    public int Behind { get; init; }
    public bool IsDetached { get; init; }

    public List<FileEntry> Staged { get; init; } = [];
    public List<FileEntry> Unstaged { get; init; } = [];

    public bool IsClean => Staged.Count == 0 && Unstaged.Count == 0;
}
