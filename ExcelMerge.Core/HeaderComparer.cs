namespace ExcelMerge.Core;

internal class HeaderComparer : IEqualityComparer<ExcelColumn>
{
    public bool Equals(ExcelColumn x, ExcelColumn y)
    {
        var valueX = x.Cells.ElementAtOrDefault(x.HeaderIndex)?.Value ?? string.Empty;
        var valueY = y.Cells.ElementAtOrDefault(y.HeaderIndex)?.Value ?? string.Empty;

        return valueX.Equals(valueY);
    }

    public int GetHashCode(ExcelColumn obj)
    {
        return obj.Cells.ElementAtOrDefault(obj.HeaderIndex)?.Value.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
