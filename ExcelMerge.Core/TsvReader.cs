namespace ExcelMerge.Core;

public class TsvReader
{
    internal static IEnumerable<ExcelRow> Read(string path)
    {
        using var sr = new StreamReader(path, Encoding.UTF8);
        var rowIndex = 0;
        while (!sr.EndOfStream)
        {
            var columnIndex = 0;
            List<ExcelCell> cells = [];
            foreach (var c in sr.ReadLine().Split('\t'))
                cells.Add(new ExcelCell(c, columnIndex, rowIndex));

            yield return new ExcelRow(rowIndex++, cells);
        }
    }
}
