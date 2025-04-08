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
    private string headerKeys = "";

    public RowComparer(HashSet<int> ignoreColumns, string headerKeys = "")
    {
        IgnoreColumns = ignoreColumns;
        this.headerKeys = headerKeys;
    }

    public bool Equals(ExcelRow x, ExcelRow y)
    {
        return GetHashCode(x).Equals(GetHashCode(y));
    }

    public int GetHashCode(ExcelRow obj)
    {
        var hash = 7;

        if (!string.IsNullOrEmpty(headerKeys))
        {
            // 복합키 사용 시
            var keyIndices = headerKeys.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int index) ? index : -1)
                .Where(i => i >= 0)
                .ToList();

            if (keyIndices.Count != 0)
            {
                foreach (int idx in keyIndices)
                {
                    if (idx < obj.Cells.Count)
                        hash = hash * 13 + obj.Cells[idx].Value.GetHashCode();
                }
                return hash;
            }
        }

        // 기존 로직 유지 (컬럼 무시 기반)
        var index = 0;
        foreach (ExcelCell cell in obj.Cells)
        {
            if (IgnoreColumns.Contains(index))
                continue;

            hash = hash * 13 + cell.Value.GetHashCode();
            index++;
        }

        return hash;
    }
}
