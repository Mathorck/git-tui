using GitTui.Interfaces;
using GitTui.Models;
using GitTui.Utils;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui.Views;

public class MainView : View
{
    private readonly IApplication _app;
    private readonly IGitService _git;
    private readonly ILocalizer _loc;

    private readonly Label _branchLabel;
    private readonly Label _aheadBehindLabel;
    private readonly Button _changesTabButton;
    private readonly Button _historyTabButton;
    private readonly ChangesView _changesView;
    private readonly HistoryView _historyView;

    public MainView(IApplication app, IGitService git, ILocalizer loc)
    {
        _app = app;
        _git = git;
        _loc = loc;

        Width = Dim.Fill();
        Height = Dim.Fill();

        var toolbar = new FrameView { X = 0, Y = 0, Width = Dim.Fill(), Height = 3 };
        _branchLabel = new Label { X = 0, Y = 0, Width = Dim.Percent(40) };
        _aheadBehindLabel = new Label { X = Pos.Right(_branchLabel) + 1, Y = 0 };

        var branchesButton = new Button { Text = loc[LocalizationKeys.MENU_BRANCH], X = Pos.AnchorEnd(56), Y = 0 };
        branchesButton.Accepting += (_, e) => { OpenBranchesDialog(); e.Handled = true; };
        var fetchButton = new Button { Text = loc[LocalizationKeys.MENU_REPOSITORY_FETCH], X = Pos.Right(branchesButton) + 1, Y = 0 };
        fetchButton.Accepting += (_, e) => { RunRemoteOperation(_git.Fetch, LocalizationKeys.FETCH_SUCCESS); e.Handled = true; };
        var pullButton = new Button { Text = loc[LocalizationKeys.MENU_REPOSITORY_PULL], X = Pos.Right(fetchButton) + 1, Y = 0 };
        pullButton.Accepting += (_, e) => { RunRemoteOperation(_git.Pull, LocalizationKeys.PULL_SUCCESS); e.Handled = true; };
        var pushButton = new Button { Text = loc[LocalizationKeys.MENU_REPOSITORY_PUSH], X = Pos.Right(pullButton) + 1, Y = 0 };
        pushButton.Accepting += (_, e) => { RunRemoteOperation(() => _git.Push(), LocalizationKeys.PUSH_SUCCESS); e.Handled = true; };

        toolbar.Add(_branchLabel, _aheadBehindLabel, branchesButton, fetchButton, pullButton, pushButton);

        _changesTabButton = new Button { Text = loc[LocalizationKeys.VIEW_CHANGES], X = 0, Y = Pos.Bottom(toolbar) };
        _changesTabButton.Accepting += (_, e) => { ShowChanges(); e.Handled = true; };
        _historyTabButton = new Button { Text = loc[LocalizationKeys.VIEW_HISTORY], X = Pos.Right(_changesTabButton) + 1, Y = Pos.Bottom(toolbar) };
        _historyTabButton.Accepting += (_, e) => { ShowHistory(); e.Handled = true; };

        var contentArea = new View { X = 0, Y = Pos.Bottom(_changesTabButton), Width = Dim.Fill(), Height = Dim.Fill() };
        _changesView = new ChangesView(app, git, loc);
        _changesView.RepositoryChanged += RefreshAll;
        _historyView = new HistoryView(app, git, loc) { Visible = false };
        contentArea.Add(_changesView, _historyView);

        Add(toolbar, _changesTabButton, _historyTabButton, contentArea);

        RefreshAll();
    }

    private void ShowChanges()
    {
        _changesView.Visible = true;
        _historyView.Visible = false;
    }

    private void ShowHistory()
    {
        _changesView.Visible = false;
        _historyView.Visible = true;
        _historyView.Refresh();
    }

    private void OpenBranchesDialog()
    {
        var dialog = new BranchesDialog(_app, _git, _loc);
        _app.Run(dialog);
        if (dialog.RepositoryChanged)
            RefreshAll();
    }

    private void RunRemoteOperation(Action operation, string successKey)
    {
        try
        {
            operation();
            RefreshAll();
            MessageBox.Query(_app, _loc[LocalizationKeys.DIALOG_SUCCESS_TITLE], _loc[successKey], _loc[LocalizationKeys.DIALOG_OK]);
        }
        catch (GitCommandException ex)
        {
            MessageBox.ErrorQuery(_app, _loc[LocalizationKeys.DIALOG_ERROR_TITLE], ex.Message, _loc[LocalizationKeys.DIALOG_OK]);
        }
    }

    public void RefreshAll()
    {
        RepositoryStatus status = _git.GetStatus();

        _branchLabel.Text = status.IsDetached
            ? _loc[LocalizationKeys.STATUS_DETACHED_LABEL, status.BranchName]
            : _loc[LocalizationKeys.STATUS_BRANCH_LABEL, status.BranchName];
        _aheadBehindLabel.Text = _loc[LocalizationKeys.STATUS_AHEAD_BEHIND, status.Ahead, status.Behind];

        _changesView.Refresh(status);
        if (_historyView.Visible)
            _historyView.Refresh();
    }
}
