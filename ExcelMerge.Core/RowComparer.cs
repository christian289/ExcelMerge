namespace ExcelMerge.Core;

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
