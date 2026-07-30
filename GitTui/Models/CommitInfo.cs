namespace GitTui.Models;

public class CommitInfo
{
    public required string Hash { get; init; }
    public required string ShortHash { get; init; }
    public required string Author { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset Date { get; init; }
    public required string Subject { get; init; }
    public string Body { get; init; } = string.Empty;

    public string SummaryLine => $"{ShortHash}  {Date:yyyy-MM-dd}  {Author,-18}  {Subject}";
}
