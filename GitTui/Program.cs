using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace GitTui;

class Program
{
    static void Main(string[] args)
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new () { Title = "Git TUI by mathorck" };
        
        window.Add(CreateNavBar());

        app.Run (window);
    }

    static View CreateNavBar()
    {
        return new MenuBar()
        {
            Menus =
            [
                new("_File", [
                    new MenuItem{ Title = "_Test" },
                    new MenuItem{ Title = "T_est" }
                ])
            ]
        };
    }
}