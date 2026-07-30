using GitTui.Interfaces;
using GitTui.Services;
using GitTui.Utils;
using GitTui.Views;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui;

class Program
{
    private static ILocalizer _localizer = new Localizer();
    private static IApplication _app = null!;
    private static IGitService _git = null!;
    private static MainView? _mainView;
    private static Window? _window;

    static void Main(string[] args)
    {
        _localizer.Reload();

        using IApplication app = Application.Create();
        _app = app;
        app.Init();

        string startPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
        string repositoryPath = FindRepositoryRoot(startPath) ?? startPath;
        _git = new GitService(repositoryPath);

        using Window window = new() { Title = _localizer[LocalizationKeys.APP_TITLE] };
        _window = window;

        window.Add(CreateNavBar());
        OpenRepository(repositoryPath);

        app.Run(window);
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    private static void OpenRepository(string path)
    {
        _git = new GitService(path);

        if (!_git.IsRepository)
        {
            MessageBox.ErrorQuery(_app, _localizer[LocalizationKeys.DIALOG_ERROR_TITLE], _localizer[LocalizationKeys.STATUS_NOT_A_REPO, path], _localizer[LocalizationKeys.DIALOG_OK]);
            return;
        }

        if (_mainView is not null)
            _window!.Remove(_mainView);

        _mainView = new MainView(_app, _git, _localizer) { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() };
        _window!.Add(_mainView);
        _window.Title = $"{_localizer[LocalizationKeys.APP_TITLE]} - {path}";
    }

    private static void PromptOpenRepository()
    {
        var dialog = new Dialog { Title = _localizer[LocalizationKeys.OPEN_REPOSITORY_TITLE], Width = Dim.Percent(60), Height = 8 };
        var label = new Label { Text = _localizer[LocalizationKeys.OPEN_REPOSITORY_PROMPT], X = 0, Y = 0 };
        var pathField = new TextField { Text = _git.RepositoryPath, X = 0, Y = Pos.Bottom(label) + 1, Width = Dim.Fill() };

        var okButton = new Button { Text = _localizer[LocalizationKeys.DIALOG_OK], IsDefault = true };
        okButton.Accepting += (_, e) =>
        {
            string? path = pathField.Text?.ToString();
            if (!string.IsNullOrWhiteSpace(path))
                OpenRepository(Path.GetFullPath(path));
            _app.RequestStop(dialog);
            e.Handled = true;
        };
        var cancelButton = new Button { Text = _localizer[LocalizationKeys.DIALOG_CANCEL] };
        cancelButton.Accepting += (_, e) => { _app.RequestStop(dialog); e.Handled = true; };

        dialog.Add(label, pathField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        _app.Run(dialog);
    }

    private static void ShowAbout()
    {
        MessageBox.Query(_app, _localizer[LocalizationKeys.ABOUT_TITLE], _localizer[LocalizationKeys.ABOUT_MESSAGE], _localizer[LocalizationKeys.DIALOG_OK]);
    }

    static View CreateNavBar()
    {
        var fileMenu = new MenuBarItem("_File", new MenuItem[]
        {
            new() { Title = _localizer[LocalizationKeys.MENU_FILE_OPEN_REPOSITORY], Action = PromptOpenRepository },
            new() { Title = _localizer[LocalizationKeys.MENU_FILE_QUIT], Action = () => _app.RequestStop(_window!) }
        });

        var repositoryMenu = new MenuBarItem("_Repository", new MenuItem[]
        {
            new() { Title = _localizer[LocalizationKeys.MENU_REPOSITORY_FETCH], Action = () => WithGitErrorHandling(_git.Fetch) },
            new() { Title = _localizer[LocalizationKeys.MENU_REPOSITORY_PULL], Action = () => WithGitErrorHandling(_git.Pull) },
            new() { Title = _localizer[LocalizationKeys.MENU_REPOSITORY_PUSH], Action = () => WithGitErrorHandling(() => _git.Push()) },
            new() { Title = _localizer[LocalizationKeys.MENU_REPOSITORY_REFRESH], Action = () => _mainView?.RefreshAll() }
        });

        var branchMenu = new MenuBarItem("_Branch", new MenuItem[]
        {
            new() { Title = _localizer[LocalizationKeys.MENU_BRANCH_SWITCH], Action = () => _mainView?.RefreshAll() }
        });

        var helpMenu = new MenuBarItem("_Help", new MenuItem[]
        {
            new() { Title = _localizer[LocalizationKeys.MENU_HELP_ABOUT], Action = ShowAbout }
        });

        return new MenuBar { Menus = [fileMenu, repositoryMenu, branchMenu, helpMenu] };
    }

    private static void WithGitErrorHandling(Action action)
    {
        try
        {
            action();
            _mainView?.RefreshAll();
        }
        catch (Models.GitCommandException ex)
        {
            MessageBox.ErrorQuery(_app, _localizer[LocalizationKeys.DIALOG_ERROR_TITLE], ex.Message, _localizer[LocalizationKeys.DIALOG_OK]);
        }
    }
}
