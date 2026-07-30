using System.Collections.ObjectModel;
using GitTui.Interfaces;
using GitTui.Models;
using GitTui.Utils;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui.Views;

public class HistoryView : View
{
    private readonly IApplication _app;
    private readonly IGitService _git;
    private readonly ILocalizer _loc;

    private readonly ListView _commitList;
    private readonly TextView _detailsView;

    private List<CommitInfo> _commits = [];

    public HistoryView(IApplication app, IGitService git, ILocalizer loc)
    {
        _app = app;
        _git = git;
        _loc = loc;

        Width = Dim.Fill();
        Height = Dim.Fill();

        var listFrame = new FrameView
        {
            Title = loc[LocalizationKeys.HISTORY_COMMITS_TITLE],
            X = 0,
            Y = 0,
            Width = Dim.Percent(38),
            Height = Dim.Fill()
        };
        _commitList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        _commitList.ValueChanged += (_, _) => ShowSelectedCommit();
        listFrame.Add(_commitList);

        var detailsFrame = new FrameView
        {
            Title = loc[LocalizationKeys.HISTORY_DETAILS_TITLE],
            X = Pos.Right(listFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _detailsView = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true };
        detailsFrame.Add(_detailsView);

        Add(listFrame, detailsFrame);
    }

    public void Refresh()
    {
        try
        {
            _commits = _git.GetLog();
        }
        catch (GitCommandException)
        {
            _commits = [];
        }

        _commitList.SetSource(new ObservableCollection<string>(
            _commits.Count > 0 ? _commits.Select(c => c.SummaryLine) : [_loc[LocalizationKeys.HISTORY_EMPTY]]));

        _detailsView.Text = "";
    }

    private void ShowSelectedCommit()
    {
        int? index = _commitList.SelectedItem;
        if (index is null || index < 0 || index >= _commits.Count)
            return;

        try
        {
            _detailsView.Text = _git.GetCommitDiff(_commits[index.Value].Hash);
        }
        catch (GitCommandException ex)
        {
            _detailsView.Text = ex.Message;
        }
    }
}
