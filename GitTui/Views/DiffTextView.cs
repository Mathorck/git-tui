using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace GitTui.Views;

/// <summary>
/// A read-only <see cref="TextView"/> that honors per-<see cref="Cell"/> colors set via <see cref="TextView.Load(List{List{Cell}})"/>.
/// The base <see cref="TextView"/> ignores Cell.Attribute when ReadOnly and always paints with the Focus scheme instead.
/// </summary>
public class DiffTextView : TextView
{
    protected override void OnDrawNormalColor(List<Cell> line, int idxCol, int idxRow) =>
        ApplyCellAttribute(line, idxCol, () => base.OnDrawNormalColor(line, idxCol, idxRow));

    protected override void OnDrawReadOnlyColor(List<Cell> line, int idxCol, int idxRow) =>
        ApplyCellAttribute(line, idxCol, () => base.OnDrawReadOnlyColor(line, idxCol, idxRow));

    private void ApplyCellAttribute(List<Cell> line, int idxCol, Action fallback)
    {
        if (idxCol >= 0 && idxCol < line.Count && line[idxCol].Attribute is Attribute attribute)
            SetAttribute(attribute);
        else
            fallback();
    }
}
