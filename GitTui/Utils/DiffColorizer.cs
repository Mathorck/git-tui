using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace GitTui.Utils;

public static class DiffColorizer
{
    public static List<List<Cell>> Colorize(string diff, Attribute normal)
    {
        var added = new Attribute(ColorName16.BrightGreen, normal.Background);
        var removed = new Attribute(ColorName16.BrightRed, normal.Background);
        var hunk = new Attribute(ColorName16.BrightCyan, normal.Background);
        var header = new Attribute(ColorName16.BrightYellow, normal.Background);

        string[] lines = diff.Replace("\r\n", "\n").Split('\n');
        var result = new List<List<Cell>>(lines.Length);

        foreach (string line in lines)
        {
            Attribute attribute = line switch
            {
                _ when line.StartsWith("+++") || line.StartsWith("---") => header,
                _ when line.StartsWith("diff --git") || line.StartsWith("index ") => header,
                _ when line.StartsWith('+') => added,
                _ when line.StartsWith('-') => removed,
                _ when line.StartsWith("@@") => hunk,
                _ => normal
            };

            var cellLine = new List<Cell>(line.Length);
            foreach (char c in line)
                cellLine.Add(new Cell(attribute, false, c.ToString()));
            result.Add(cellLine);
        }

        return result;
    }
}
