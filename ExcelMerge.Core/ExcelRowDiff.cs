namespace ExcelMerge.Core;

public class ExcelRowDiff
{
    public int Index { get; private set; }
    public SortedDictionary<int, ExcelCellDiff> Cells { get; private set; }

    public ExcelRowDiff(int index)
    {
        Index = index;
        Cells = [];
    }

    public ExcelCellDiff CreateCell(ExcelCell src, ExcelCell dst, int columnIndex, ExcelCellStatus status)
    {
        ExcelCellDiff cell = new(columnIndex, Index, src, dst, status);
        Cells.Add(cell.ColumnIndex, cell);

        return cell;
    }

    public bool IsModified() => Cells.Any(c => c.Value.Status != ExcelCellStatus.None);

    public bool IsAdded() => Cells.All(c => c.Value.Status == ExcelCellStatus.Added);

    public bool IsRemoved() => Cells.All(c => c.Value.Status == ExcelCellStatus.Removed);

    public int ModifiedCellCount => Cells.Count(c => c.Value.Status != ExcelCellStatus.None);


    // TODO: Add row status field and implemnt UpdateStaus method.
}
