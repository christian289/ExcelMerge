namespace ExcelMerge.Core;

public class ExcelColumn : IEquatable<ExcelColumn>
{
    public List<ExcelCell> Cells { get; private set; }
    public int HeaderIndex { get; set; }

    public ExcelColumn()
    {
        Cells = [];
    }

    public ExcelColumn(IEnumerable<ExcelCell> cells)
    {
        Cells = [.. cells];
    }

    public override bool Equals(object obj)
    {
        var other = obj as ExcelColumn;

        return Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = 7;
        foreach (var cell in Cells)
            hash = hash * 13 + cell.Value.GetHashCode();

        return hash;
    }

    public bool Equals(ExcelColumn other)
    {
        if (other is null)
            return false;

        return GetHashCode() == other.GetHashCode();
    }

    public bool IsBlank()
    {
        return Cells.All(c => string.IsNullOrEmpty(c.Value));
    }
}
