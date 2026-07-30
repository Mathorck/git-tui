using System.Collections.ObjectModel;
using GitTui.Interfaces;
using GitTui.Models;
using GitTui.Utils;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui.Views;

public class BranchesDialog : Dialog
{
    private readonly IApplication _app;
    private readonly IGitService _git;
    private readonly ILocalizer _loc;

    private readonly TextField _filterField;
    private readonly ListView _branchList;

    private List<Branch> _allBranches = [];
    private List<Branch> _filteredBranches = [];

    public bool RepositoryChanged { get; private set; }

    public BranchesDialog(IApplication app, IGitService git, ILocalizer loc)
    {
        _app = app;
        _git = git;
        _loc = loc;

        Title = loc[LocalizationKeys.BRANCH_DIALOG_TITLE];
        Width = Dim.Percent(70);
        Height = Dim.Percent(70);

        _filterField = new TextField { X = 0, Y = 0, Width = Dim.Fill() };
        _filterField.TextChanged += (_, _) => ApplyFilter();

        _branchList = new ListView { X = 0, Y = Pos.Bottom(_filterField) + 1, Width = Dim.Fill(), Height = Dim.Fill(1) };
        _branchList.Accepting += (_, e) => { Checkout(); e.Handled = true; };

        var checkoutButton = new Button { Text = loc[LocalizationKeys.BRANCH_CHECKOUT], IsDefault = true };
        checkoutButton.Accepting += (_, e) => { Checkout(); e.Handled = true; };

        var newButton = new Button { Text = loc[LocalizationKeys.BRANCH_NEW] };
        newButton.Accepting += (_, e) => { CreateNew(); e.Handled = true; };

        var cancelButton = new Button { Text = loc[LocalizationKeys.BRANCH_CANCEL] };
        cancelButton.Accepting += (_, e) => { _app.RequestStop(this); e.Handled = true; };

        Add(_filterField, _branchList);
        AddButton(checkoutButton);
        AddButton(newButton);
        AddButton(cancelButton);

        LoadBranches();
    }

    private void LoadBranches()
    {
        _allBranches = _git.GetBranches();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string filter = _filterField.Text?.ToString() ?? "";
        _filteredBranches = string.IsNullOrWhiteSpace(filter)
            ? _allBranches
            : _allBranches.Where(b => b.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        _branchList.SetSource(new ObservableCollection<string>(_filteredBranches.Select(FormatBranch)));
    }

    private string FormatBranch(Branch branch)
    {
        string suffix = branch.IsCurrent
            ? _loc[LocalizationKeys.BRANCH_CURRENT_SUFFIX]
            : branch.IsRemote ? _loc[LocalizationKeys.BRANCH_REMOTE_SUFFIX] : "";
        return $"{branch.Name}{suffix}";
    }

    private void Checkout()
    {
        int? index = _branchList.SelectedItem;
        if (index is null || index < 0 || index >= _filteredBranches.Count)
            return;

        Branch branch = _filteredBranches[index.Value];
        try
        {
            _git.SwitchBranch(branch.Name);
            RepositoryChanged = true;
            _app.RequestStop(this);
        }
        catch (GitCommandException ex)
        {
            MessageBox.ErrorQuery(_app, _loc[LocalizationKeys.DIALOG_ERROR_TITLE], ex.Message, _loc[LocalizationKeys.DIALOG_OK]);
        }
    }

    private void CreateNew()
    {
        string name = _filterField.Text?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            _git.CreateBranch(name);
            RepositoryChanged = true;
            _app.RequestStop(this);
        }
        catch (GitCommandException ex)
        {
            MessageBox.ErrorQuery(_app, _loc[LocalizationKeys.DIALOG_ERROR_TITLE], ex.Message, _loc[LocalizationKeys.DIALOG_OK]);
        }
    }
}
