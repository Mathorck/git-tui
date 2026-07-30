using System.Collections.ObjectModel;
using GitTui.Interfaces;
using GitTui.Models;
using GitTui.Utils;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui.Views;

public class ChangesView : View
{
    private readonly IApplication _app;
    private readonly IGitService _git;
    private readonly ILocalizer _loc;

    private readonly ListView _unstagedList;
    private readonly ListView _stagedList;
    private readonly TextView _diffView;
    private readonly TextField _summaryField;
    private readonly TextView _descriptionView;
    private readonly Button _commitButton;

    private List<FileEntry> _unstagedEntries = [];
    private List<FileEntry> _stagedEntries = [];
    private RepositoryStatus? _status;

    public event Action? RepositoryChanged;

    public ChangesView(IApplication app, IGitService git, ILocalizer loc)
    {
        _app = app;
        _git = git;
        _loc = loc;

        Width = Dim.Fill();
        Height = Dim.Fill();

        var leftPanel = new View { X = 0, Y = 0, Width = Dim.Percent(38), Height = Dim.Fill() };

        var unstagedFrame = new FrameView
        {
            Title = loc[LocalizationKeys.CHANGES_UNSTAGED_TITLE],
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(42)
        };
        _unstagedList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1) };
        _unstagedList.ValueChanged += (_, _) => ShowDiffForSelection(_unstagedList, _unstagedEntries);
        _unstagedList.Accepting += (_, e) =>
        {
            ToggleUnstagedSelection();
            e.Handled = true;
        };
        var stageAllButton = new Button { Text = loc[LocalizationKeys.CHANGES_STAGE_ALL], X = 0, Y = Pos.Bottom(_unstagedList) };
        stageAllButton.Accepting += (_, e) => { StageAll(); e.Handled = true; };
        var stageButton = new Button { Text = loc[LocalizationKeys.CHANGES_STAGE_SELECTED], X = Pos.Right(stageAllButton) + 1, Y = Pos.Bottom(_unstagedList) };
        stageButton.Accepting += (_, e) => { ToggleUnstagedSelection(); e.Handled = true; };
        var discardButton = new Button { Text = loc[LocalizationKeys.CHANGES_DISCARD_SELECTED], X = Pos.Right(stageButton) + 1, Y = Pos.Bottom(_unstagedList) };
        discardButton.Accepting += (_, e) => { DiscardSelected(); e.Handled = true; };
        unstagedFrame.Add(_unstagedList, stageAllButton, stageButton, discardButton);

        var stagedFrame = new FrameView
        {
            Title = loc[LocalizationKeys.CHANGES_STAGED_TITLE],
            X = 0,
            Y = Pos.Bottom(unstagedFrame),
            Width = Dim.Fill(),
            Height = Dim.Percent(42)
        };
        _stagedList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1) };
        _stagedList.ValueChanged += (_, _) => ShowDiffForSelection(_stagedList, _stagedEntries);
        _stagedList.Accepting += (_, e) =>
        {
            ToggleStagedSelection();
            e.Handled = true;
        };
        var unstageAllButton = new Button { Text = loc[LocalizationKeys.CHANGES_UNSTAGE_ALL], X = 0, Y = Pos.Bottom(_stagedList) };
        unstageAllButton.Accepting += (_, e) => { UnstageAll(); e.Handled = true; };
        var unstageButton = new Button { Text = loc[LocalizationKeys.CHANGES_UNSTAGE_SELECTED], X = Pos.Right(unstageAllButton) + 1, Y = Pos.Bottom(_stagedList) };
        unstageButton.Accepting += (_, e) => { ToggleStagedSelection(); e.Handled = true; };
        stagedFrame.Add(_stagedList, unstageAllButton, unstageButton);

        var commitFrame = new FrameView
        {
            Title = "",
            X = 0,
            Y = Pos.Bottom(stagedFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _summaryField = new TextField { X = 0, Y = 0, Width = Dim.Fill() };
        _descriptionView = new TextView { X = 0, Y = Pos.Bottom(_summaryField) + 1, Width = Dim.Fill(), Height = Dim.Fill(2) };
        _commitButton = new Button { Text = loc[LocalizationKeys.CHANGES_COMMIT_BUTTON, ""], X = 0, Y = Pos.Bottom(_descriptionView) + 1 };
        _commitButton.Accepting += (_, e) => { CreateCommit(); e.Handled = true; };
        commitFrame.Add(_summaryField, _descriptionView, _commitButton);

        leftPanel.Add(unstagedFrame, stagedFrame, commitFrame);

        var diffFrame = new FrameView
        {
            Title = loc[LocalizationKeys.CHANGES_DIFF_TITLE],
            X = Pos.Right(leftPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _diffView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        diffFrame.Add(_diffView);

        Add(leftPanel, diffFrame);
    }

    public void Refresh(RepositoryStatus status)
    {
        _status = status;
        _unstagedEntries = status.Unstaged;
        _stagedEntries = status.Staged;

        _unstagedList.SetSource(new ObservableCollection<string>(_unstagedEntries.Select(FormatEntry)));
        _stagedList.SetSource(new ObservableCollection<string>(_stagedEntries.Select(FormatEntry)));

        _commitButton.Text = _loc[LocalizationKeys.CHANGES_COMMIT_BUTTON, status.BranchName];
        _commitButton.Enabled = _stagedEntries.Count > 0;

        _diffView.Text = _loc[LocalizationKeys.CHANGES_EMPTY_DIFF];
    }

    private static string FormatEntry(FileEntry entry) => $"[{StatusGlyph(entry.Kind)}] {entry.DisplayName}";

    private static string StatusGlyph(FileChangeKind kind) => kind switch
    {
        FileChangeKind.Modified => "M",
        FileChangeKind.Added => "A",
        FileChangeKind.Deleted => "D",
        FileChangeKind.Renamed => "R",
        FileChangeKind.Copied => "C",
        FileChangeKind.Untracked => "?",
        FileChangeKind.Conflicted => "U",
        FileChangeKind.TypeChanged => "T",
        _ => " "
    };

    private void ShowDiffForSelection(ListView list, List<FileEntry> entries)
    {
        int? index = list.SelectedItem;
        if (index is null || index < 0 || index >= entries.Count)
            return;

        try
        {
            SetDiffText(_git.GetDiff(entries[index.Value]));
        }
        catch (GitCommandException ex)
        {
            _diffView.Text = ex.Message;
        }
    }

    private void SetDiffText(string diff)
    {
        Terminal.Gui.Drawing.Attribute normal = _diffView.GetScheme().GetAttributeForRole(VisualRole.Normal, null);
        _diffView.Load(DiffColorizer.Colorize(diff, normal));
    }

    private void ToggleUnstagedSelection()
    {
        int? index = _unstagedList.SelectedItem;
        if (index is null || index < 0 || index >= _unstagedEntries.Count)
            return;

        WithErrorHandling(() =>
        {
            _git.StageFile(_unstagedEntries[index.Value].Path);
            RepositoryChanged?.Invoke();
        });
    }

    private void ToggleStagedSelection()
    {
        int? index = _stagedList.SelectedItem;
        if (index is null || index < 0 || index >= _stagedEntries.Count)
            return;

        WithErrorHandling(() =>
        {
            _git.UnstageFile(_stagedEntries[index.Value].Path);
            RepositoryChanged?.Invoke();
        });
    }

    private void StageAll() => WithErrorHandling(() =>
    {
        _git.StageAll();
        RepositoryChanged?.Invoke();
    });

    private void UnstageAll() => WithErrorHandling(() =>
    {
        _git.UnstageAll();
        RepositoryChanged?.Invoke();
    });

    private void DiscardSelected()
    {
        int? index = _unstagedList.SelectedItem;
        if (index is null || index < 0 || index >= _unstagedEntries.Count)
            return;

        FileEntry entry = _unstagedEntries[index.Value];
        int? result = MessageBox.Query(
            _app,
            _loc[LocalizationKeys.CHANGES_DISCARD_CONFIRM_TITLE],
            _loc[LocalizationKeys.CHANGES_DISCARD_CONFIRM_MESSAGE, entry.Path],
            _loc[LocalizationKeys.DIALOG_YES], _loc[LocalizationKeys.DIALOG_NO]);

        if (result != 0)
            return;

        WithErrorHandling(() =>
        {
            _git.DiscardFile(entry);
            RepositoryChanged?.Invoke();
        });
    }

    private void CreateCommit()
    {
        string subject = _summaryField.Text?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(subject))
            return;

        string body = _descriptionView.Text?.ToString() ?? "";

        WithErrorHandling(() =>
        {
            _git.Commit(subject, body);
            _summaryField.Text = "";
            _descriptionView.Text = "";
            RepositoryChanged?.Invoke();
        });
    }

    private void WithErrorHandling(Action action)
    {
        try
        {
            action();
        }
        catch (GitCommandException ex)
        {
            MessageBox.ErrorQuery(_app, _loc[LocalizationKeys.DIALOG_ERROR_TITLE], ex.Message, _loc[LocalizationKeys.DIALOG_OK]);
        }
    }
}
