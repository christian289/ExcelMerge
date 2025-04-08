namespace ExcelMerge;

public class ExcelColumn : IEquatable<ExcelColumn>
{
    public List<ExcelCell> Cells { get; private set; }
    public int HeaderIndex { get; set; }

    public ExcelColumn()
    {
        Cells = new List<ExcelCell>();
    }

    public ExcelColumn(IEnumerable<ExcelCell> cells)
    {
        Cells = cells.ToList();
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
        {
            hash = hash * 13 + cell.Value.GetHashCode();
        }

        return hash;
    }

    public bool Equals(ExcelColumn other)
    {
        if (other == null)
            return false;

        return GetHashCode() == other.GetHashCode();
    }

    public bool IsBlank()
    {
        return Cells.All(c => string.IsNullOrEmpty(c.Value));
    }
}

internal class HeaderComparer : IEqualityComparer<ExcelColumn>
{
    private HashSet<int> headerIndices;

    public HeaderComparer(string headerIndicesStr = "")
    {
        headerIndices = new HashSet<int>();
        if (!string.IsNullOrEmpty(headerIndicesStr))
        {
            var indices = headerIndicesStr.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int index) ? index : -1)
                .Where(i => i >= 0);

            foreach (var idx in indices)
                headerIndices.Add(idx);
        }

        // 인덱스가 비어있으면 기본값 추가
        if (!headerIndices.Any())
            headerIndices.Add(0);
    }

    public bool Equals(ExcelColumn x, ExcelColumn y)
    {
        if (x == null || y == null)
            return false;

        foreach (var headerIndex in headerIndices)
        {
            var valueX = x.Cells.ElementAtOrDefault(headerIndex)?.Value ?? string.Empty;
            var valueY = y.Cells.ElementAtOrDefault(headerIndex)?.Value ?? string.Empty;

            if (!valueX.Equals(valueY))
                return false;
        }

        return true;
    }

    public int GetHashCode(ExcelColumn obj)
    {
        int hashCode = 17;

        foreach (var headerIndex in headerIndices)
        {
            string value = obj.Cells.ElementAtOrDefault(headerIndex)?.Value ?? string.Empty;
            hashCode = hashCode * 31 + value.GetHashCode();
        }

        return hashCode;
    }
}
