namespace GitTui.Models;

public class GitCommandException(string command, int exitCode, string errorOutput)
    : Exception($"git {command} failed ({exitCode}): {errorOutput}")
{
    public string Command { get; } = command;
    public int ExitCode { get; } = exitCode;
    public string ErrorOutput { get; } = errorOutput;
}
