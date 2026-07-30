using GitTui.Models;

namespace GitTui.Interfaces;

public interface IGitService
{
    string RepositoryPath { get; }
    bool IsRepository { get; }

    RepositoryStatus GetStatus();

    string GetDiff(FileEntry entry);

    void StageFile(string path);
    void StageAll();
    void UnstageFile(string path);
    void UnstageAll();
    void DiscardFile(FileEntry entry);

    void Commit(string subject, string body = "");

    List<Branch> GetBranches();
    void CreateBranch(string name, bool checkout = true);
    void SwitchBranch(string name);

    List<CommitInfo> GetLog(int maxCount = 200);
    string GetCommitDiff(string hash);

    void Fetch();
    void Pull();
    void Push(bool setUpstream = false);
}
