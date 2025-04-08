namespace ExcelMerge;

public class ExcelRow : IEquatable<ExcelRow>
{
    public int Index { get; private set; }
    public List<ExcelCell> Cells { get; private set; }

    public ExcelRow(int index, IEnumerable<ExcelCell> cells)
    {
        Index = index;
        Cells = cells.ToList();
    }

    public override bool Equals(object obj)
    {
        var other = obj as ExcelRow;

        return Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = 7;
        foreach (ExcelCell cell in Cells)
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

    public void UpdateCells(IEnumerable<ExcelCell> cells) => Cells = [.. cells];
}

internal class RowComparer : IEqualityComparer<ExcelRow>
{
    public HashSet<int> IgnoreColumns { get; private set; }

    public RowComparer(HashSet<int> ignoreColumns)
    {
        IgnoreColumns = ignoreColumns;
    }

    public bool Equals(ExcelRow x, ExcelRow y)
    {
        return GetHashCode(x).Equals(GetHashCode(y));
    }

    public int GetHashCode(ExcelRow obj)
    {
        var hash = 7;
        var index = 0;
        foreach (var cell in obj.Cells)
        {
            if (IgnoreColumns.Contains(index))
                continue;

            hash = hash * 13 + cell.Value.GetHashCode();

            index++;
        }

        return hash;
    }
}
