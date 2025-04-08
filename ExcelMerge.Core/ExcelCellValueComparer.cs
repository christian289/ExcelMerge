namespace ExcelMerge.Core;

public class ExcelCellValueComparer : IEqualityComparer<ExcelCell>
{
    public bool Equals(ExcelCell x, ExcelCell y)
    {
        if (x is null || y is null)
            return false;

        return x.Value.Equals(y.Value);
    }

    public int GetHashCode(ExcelCell obj) => obj.Value.GetHashCode();
}
