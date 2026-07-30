namespace GitTui.Models;

public enum FileChangeKind
{
    Modified,
    Added,
    Deleted,
    Renamed,
    Copied,
    Untracked,
    Conflicted,
    TypeChanged
}

public class FileEntry
{
    public required string Path { get; init; }
    public string? OriginalPath { get; init; }
    public required char IndexStatus { get; init; }
    public required char WorktreeStatus { get; init; }
    public bool IsStaged { get; init; }

    public FileChangeKind Kind => ClassifyStatus(IsStaged ? IndexStatus : WorktreeStatus);

    public string DisplayName => OriginalPath is null ? Path : $"{OriginalPath} -> {Path}";

    private static FileChangeKind ClassifyStatus(char status) => status switch
    {
        'M' => FileChangeKind.Modified,
        'A' => FileChangeKind.Added,
        'D' => FileChangeKind.Deleted,
        'R' => FileChangeKind.Renamed,
        'C' => FileChangeKind.Copied,
        'T' => FileChangeKind.TypeChanged,
        'U' => FileChangeKind.Conflicted,
        '?' => FileChangeKind.Untracked,
        _ => FileChangeKind.Modified
    };
}
