using NPOI.SS.UserModel;

namespace ExcelMerge;

internal class ExcelReader
{
    public static IEnumerable<ExcelRow> Read(ISheet sheet)
    {
        var actualRowIndex = 0;
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            IRow row = sheet.GetRow(rowIndex);

            List<ExcelCell> cells = [];
            if (row != null)
            {
                for (int columnIndex = 0; columnIndex < row.LastCellNum; columnIndex++)
                {
                    ICell cell = row.GetCell(columnIndex);
                    string stringValue = ExcelUtility.GetCellStringValue(cell);

                    cells.Add(new ExcelCell(stringValue, columnIndex, rowIndex));
                }
            }

            yield return new ExcelRow(actualRowIndex++, cells);
        }
    }
}
