using System.Diagnostics;
using System.Text;
using GitTui.Interfaces;
using GitTui.Models;

namespace GitTui.Services;

public class GitService : IGitService
{
    public string RepositoryPath { get; }

    public GitService(string repositoryPath)
    {
        RepositoryPath = repositoryPath;
    }

    public bool IsRepository
    {
        get
        {
            try
            {
                return RunGit(["rev-parse", "--is-inside-work-tree"]).Trim() == "true";
            }
            catch
            {
                return false;
            }
        }
    }

    public RepositoryStatus GetStatus()
    {
        string output = RunGit(["status", "--porcelain=v2", "--branch", "--untracked-files=all"]);
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string branchName = "HEAD";
        string? upstream = null;
        int ahead = 0, behind = 0;
        bool detached = false;
        var staged = new List<FileEntry>();
        var unstaged = new List<FileEntry>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
                continue;

            switch (line[0])
            {
                case '#':
                    ParseHeader(line, ref branchName, ref upstream, ref ahead, ref behind, ref detached);
                    break;
                case '1':
                    ParseOrdinaryEntry(line, staged, unstaged);
                    break;
                case '2':
                    ParseRenamedEntry(line, staged, unstaged);
                    break;
                case 'u':
                    ParseUnmergedEntry(line, unstaged);
                    break;
                case '?':
                    ParseUntrackedEntry(line, unstaged);
                    break;
                // '!' ignored entries are skipped entirely
            }
        }

        return new RepositoryStatus
        {
            BranchName = branchName,
            UpstreamName = upstream,
            Ahead = ahead,
            Behind = behind,
            IsDetached = detached,
            Staged = staged,
            Unstaged = unstaged
        };
    }

    private static void ParseHeader(string line, ref string branchName, ref string? upstream, ref int ahead, ref int behind, ref bool detached)
    {
        // # branch.head <name>|(detached)
        // # branch.upstream <name>
        // # branch.ab +<ahead> -<behind>
        string content = line[2..];
        if (content.StartsWith("branch.head "))
        {
            string head = content["branch.head ".Length..];
            branchName = head;
            if (head == "(detached)")
                detached = true;
        }
        else if (content.StartsWith("branch.upstream "))
        {
            upstream = content["branch.upstream ".Length..];
        }
        else if (content.StartsWith("branch.ab "))
        {
            string[] parts = content["branch.ab ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.StartsWith('+') && int.TryParse(part[1..], out int a))
                    ahead = a;
                else if (part.StartsWith('-') && int.TryParse(part[1..], out int b))
                    behind = b;
            }
        }
    }

    private static void ParseOrdinaryEntry(string line, List<FileEntry> staged, List<FileEntry> unstaged)
    {
        // 1 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <path>
        string[] parts = line.Split(' ', 9, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 9)
            return;

        string xy = parts[1];
        string path = parts[8];
        AddEntry(xy[0], xy[1], path, null, staged, unstaged);
    }

    private static void ParseRenamedEntry(string line, List<FileEntry> staged, List<FileEntry> unstaged)
    {
        // 2 <XY> <sub> <mH> <mI> <mW> <hH> <hI> <X><score> <path><tab><origPath>
        string[] parts = line.Split(' ', 10, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 10)
            return;

        string xy = parts[1];
        string[] paths = parts[9].Split('\t');
        string path = paths[0];
        string? origPath = paths.Length > 1 ? paths[1] : null;
        AddEntry(xy[0], xy[1], path, origPath, staged, unstaged);
    }

    private static void ParseUnmergedEntry(string line, List<FileEntry> unstaged)
    {
        // u <XY> <sub> <m1> <m2> <m3> <mW> <hH> <h1> <h2> <path>
        string[] parts = line.Split(' ', 11, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 11)
            return;

        string path = parts[10];
        unstaged.Add(new FileEntry { Path = path, IndexStatus = 'U', WorktreeStatus = 'U', IsStaged = false });
    }

    private static void ParseUntrackedEntry(string line, List<FileEntry> unstaged)
    {
        string path = line[2..];
        unstaged.Add(new FileEntry { Path = path, IndexStatus = '?', WorktreeStatus = '?', IsStaged = false });
    }

    private static void AddEntry(char x, char y, string path, string? origPath, List<FileEntry> staged, List<FileEntry> unstaged)
    {
        if (x != '.')
            staged.Add(new FileEntry { Path = path, OriginalPath = origPath, IndexStatus = x, WorktreeStatus = y, IsStaged = true });

        if (y != '.')
            unstaged.Add(new FileEntry { Path = path, OriginalPath = origPath, IndexStatus = x, WorktreeStatus = y, IsStaged = false });
    }

    public string GetDiff(FileEntry entry)
    {
        if (entry.Kind == FileChangeKind.Untracked)
            return RunGit(["diff", "--no-index", "--", "/dev/null", entry.Path], allowedExitCodes: [0, 1]);

        return entry.IsStaged
            ? RunGit(["diff", "--cached", "--", entry.Path])
            : RunGit(["diff", "--", entry.Path]);
    }

    public void StageFile(string path) => RunGit(["add", "--", path]);

    public void StageAll() => RunGit(["add", "-A"]);

    public void UnstageFile(string path) => RunGit(["restore", "--staged", "--", path]);

    public void UnstageAll() => RunGit(["restore", "--staged", "."]);

    public void DiscardFile(FileEntry entry)
    {
        if (entry.Kind == FileChangeKind.Untracked)
            RunGit(["clean", "-f", "--", entry.Path]);
        else
            RunGit(["checkout", "--", entry.Path]);
    }

    public void Commit(string subject, string body = "")
    {
        string message = string.IsNullOrWhiteSpace(body) ? subject : $"{subject}\n\n{body}";
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, message);
            RunGit(["commit", "-F", tempFile]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public List<Branch> GetBranches()
    {
        const string sep = "\x1f";
        string output = RunGit([
            "for-each-ref",
            $"--format=%(HEAD){sep}%(refname:short){sep}%(upstream:short){sep}%(refname)",
            "refs/heads", "refs/remotes"
        ]);

        var branches = new List<Branch>();
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = line.Split(sep);
            if (fields.Length < 4)
                continue;

            string refName = fields[3];
            if (refName.EndsWith("/HEAD"))
                continue;

            branches.Add(new Branch
            {
                Name = fields[1],
                IsCurrent = fields[0] == "*",
                IsRemote = refName.StartsWith("refs/remotes/"),
                Upstream = string.IsNullOrEmpty(fields[2]) ? null : fields[2]
            });
        }

        return branches;
    }

    public void CreateBranch(string name, bool checkout = true)
    {
        RunGit(checkout ? ["checkout", "-b", name] : ["branch", name]);
    }

    public void SwitchBranch(string name) => RunGit(["checkout", name]);

    public List<CommitInfo> GetLog(int maxCount = 200)
    {
        const string recordSep = "\x1e";
        const string fieldSep = "\x1f";
        string output = RunGit([
            "log",
            $"--max-count={maxCount}",
            $"--pretty=format:%H{fieldSep}%h{fieldSep}%an{fieldSep}%ae{fieldSep}%aI{fieldSep}%s{recordSep}"
        ]);

        var commits = new List<CommitInfo>();
        foreach (string record in output.Split(recordSep, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] fields = record.TrimStart('\n').Split(fieldSep);
            if (fields.Length < 6)
                continue;

            if (!DateTimeOffset.TryParse(fields[4], out DateTimeOffset date))
                date = DateTimeOffset.MinValue;

            commits.Add(new CommitInfo
            {
                Hash = fields[0],
                ShortHash = fields[1],
                Author = fields[2],
                Email = fields[3],
                Date = date,
                Subject = fields[5]
            });
        }

        return commits;
    }

    public string GetCommitDiff(string hash) => RunGit(["show", hash]);

    public void Fetch() => RunGit(["fetch", "--all", "--prune"]);

    public void Pull() => RunGit(["pull"]);

    public void Push(bool setUpstream = false)
    {
        if (setUpstream)
        {
            RepositoryStatus status = GetStatus();
            RunGit(["push", "--set-upstream", "origin", status.BranchName]);
        }
        else
        {
            RunGit(["push"]);
        }
    }

    private string RunGit(IEnumerable<string> arguments, int[]? allowedExitCodes = null)
    {
        allowedExitCodes ??= [0];

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(RepositoryPath);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("core.quotepath=false");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("color.ui=false");
        foreach (string arg in arguments)
            psi.ArgumentList.Add(arg);

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git process.");
        process.StandardInput.Close();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!allowedExitCodes.Contains(process.ExitCode))
            throw new GitCommandException(string.Join(' ', arguments), process.ExitCode, stderr.Trim());

        return stdout;
    }
}
