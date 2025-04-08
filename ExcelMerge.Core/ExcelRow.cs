namespace ExcelMerge.Core;

public class ExcelRow : IEquatable<ExcelRow>
{
    public int Index { get; private set; }
    public List<ExcelCell> Cells { get; private set; }

    public ExcelRow(int index, IEnumerable<ExcelCell> cells)
    {
        Index = index;
        Cells = [.. cells];
    }

    public override bool Equals(object obj)
    {
        var other = obj as ExcelRow;

        return Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = 7;
        foreach (var cell in Cells)
            hash = hash * 13 + cell.Value.GetHashCode();

        return hash;
    }

    public bool Equals(ExcelRow other)
    {
        if (other is null)
            return false;

        return GetHashCode() == other.GetHashCode();
    }

    public bool IsBlank() => Cells.All(c => string.IsNullOrEmpty(c.Value));

    public void UpdateCells(IEnumerable<ExcelCell> cells) => Cells = cells.ToList();
}
