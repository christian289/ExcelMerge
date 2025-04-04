using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using NetDiff;
using SKCore.Collection;
using System.Security.Cryptography;

namespace ExcelMerge
{
    public class ExcelSheet
    {
        public int Index { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public SortedDictionary<int, ExcelRow> Rows { get; private set; }

        public ExcelSheet()
        {
            Rows = new SortedDictionary<int, ExcelRow>();
        }

        public static ExcelSheet CreateEmpty(string name, int index)
        {
            var sheet = new ExcelSheet
            {
                Name = name,
                Index = index
            };

            return sheet;
        }

        public static ExcelSheet Create(ISheet srcSheet, ExcelSheetReadConfig config)
        {
            var rows = ExcelReader.Read(srcSheet);
            var sheet = CreateSheet(rows, config);
            sheet.Name = srcSheet.SheetName;
            sheet.Index = srcSheet.Workbook.GetSheetIndex(sheet.Name);

            return sheet;
        }

        public static ExcelSheet CreateFromCsv(string path, ExcelSheetReadConfig config)
        {
            var rows = CsvReader.Read(path);

            var sheet = CreateSheet(rows, config);
            sheet.Name = "csv";

            return sheet;

        }

        public static ExcelSheet CreateFromTsv(string path, ExcelSheetReadConfig config)
        {
            var rows = TsvReader.Read(path);

            var sheet = CreateSheet(rows, config);
            sheet.Name = "tsv";

            return sheet;
        }

        private static ExcelSheet CreateSheet(IEnumerable<ExcelRow> rows, ExcelSheetReadConfig config)
        {
            var sheet = CreateSheet(rows);

            if (config.TrimFirstBlankRows)
                sheet.TrimFirstBlankRows();

            if (config.TrimFirstBlankColumns)
                sheet.TrimFirstBlankColumns();

            if (config.TrimLastBlankRows)
                sheet.TrimLastBlankRows();

            if (config.TrimLastBlankColumns)
                sheet.TrimLastBlankColumns();

            return sheet;
        }

        public void TrimFirstBlankRows()
        {
            var rows = new SortedDictionary<int, ExcelRow>();
            var index = 0;
            foreach (var row in Rows.SkipWhile(r => r.Value.IsBlank()))
            {
                rows.Add(index, new ExcelRow(index, row.Value.Cells));
                index++;
            }

            Rows = rows;
        }

        public void TrimFirstBlankColumns()
        {
            var columns = CreateColumns();
            var indices = columns.Select((v, i) => new { v, i }).TakeWhile(c => c.v.IsBlank()).Select(c => c.i);

            foreach (var i in indices)
                RemoveColumn(i);
        }

        public void TrimLastBlankRows()
        {
            var rows = new SortedDictionary<int, ExcelRow>();
            var index = 0;
            foreach (var row in Rows.Reverse().SkipWhile(r => r.Value.IsBlank()).Reverse())
            {
                rows.Add(index, new ExcelRow(index, row.Value.Cells));
                index++;
            }

            Rows = rows;
        }

        public void TrimLastBlankColumns()
        {
            var columns = CreateColumns();
            var indices = columns.Select((v, i) => new { v, i }).Reverse().TakeWhile(c => c.v.IsBlank()).Select(c => c.i);

            foreach (var i in indices)
                RemoveColumn(i);
        }

        public void RemoveColumn(int column)
        {
            foreach (var row in Rows)
            {
                if (row.Value.Cells.Count > column)
                    row.Value.Cells.RemoveAt(column);
            }
        }

        private static ExcelSheet CreateSheet(IEnumerable<ExcelRow> rows)
        {
            var sheet = new ExcelSheet();
            foreach (var row in rows)
            {
                sheet.Rows.Add(row.Index, row);
            }

            return sheet;
        }

        public static ExcelSheetDiff Diff(ExcelSheet src, ExcelSheet dst, ExcelSheetDiffConfig config)
        {
            var srcColumns = src.CreateColumns();
            var dstColumns = dst.CreateColumns();
            var columnStatusMap = CreateColumnStatusMap(srcColumns, dstColumns, config);

            if (config.UseKeyColumn && !string.IsNullOrEmpty(config.KeyColumnName))
            {
                return DiffWithKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
            }
            else
            {
                return DiffWithoutKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
            }
        }

        private static ExcelSheetDiff DiffWithKeyColumn(ExcelSheet src, ExcelSheet dst, ExcelSheetDiffConfig config,
                                                IEnumerable<ExcelColumn> srcColumns, IEnumerable<ExcelColumn> dstColumns,
                                                Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            // 1. 키 컬럼에 해당하는 컬럼 인덱스 찾기
            int? srcKeyColumnIndex = null;
            int? dstKeyColumnIndex = null;

            // 키 컬럼 이름이 있는 칸을 헤더에서 찾기
            if (config.SrcHeaderIndex >= 0)
            {
                int colIndex = 0;
                foreach (var column in srcColumns)
                {
                    if (column.Cells.Count > config.SrcHeaderIndex &&
                        column.Cells[config.SrcHeaderIndex].Value == config.KeyColumnName)
                    {
                        srcKeyColumnIndex = colIndex;
                        break;
                    }
                    colIndex++;
                }
            }

            if (config.DstHeaderIndex >= 0)
            {
                int colIndex = 0;
                foreach (var column in dstColumns)
                {
                    if (column.Cells.Count > config.DstHeaderIndex &&
                        column.Cells[config.DstHeaderIndex].Value == config.KeyColumnName)
                    {
                        dstKeyColumnIndex = colIndex;
                        break;
                    }
                    colIndex++;
                }
            }

            // 키 컬럼을 찾지 못한 경우 기본 형태로 비교
            if (!srcKeyColumnIndex.HasValue || !dstKeyColumnIndex.HasValue)
            {
                return DiffWithoutKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
            }

            // 2. 키값에 따라 맵 생성
            var srcKeyMap = new Dictionary<string, ExcelRow>();
            var dstKeyMap = new Dictionary<string, ExcelRow>();

            // 소스와 대상 Row를 키 값에 따라 보관
            foreach (var row in src.Rows.Values)
            {
                if (srcKeyColumnIndex.Value < row.Cells.Count)
                {
                    var keyValue = row.Cells[srcKeyColumnIndex.Value].Value;
                    if (!string.IsNullOrEmpty(keyValue) && !srcKeyMap.ContainsKey(keyValue))
                    {
                        srcKeyMap.Add(keyValue, row);
                    }
                }
            }

            foreach (var row in dst.Rows.Values)
            {
                if (dstKeyColumnIndex.Value < row.Cells.Count)
                {
                    var keyValue = row.Cells[dstKeyColumnIndex.Value].Value;
                    if (!string.IsNullOrEmpty(keyValue) && !dstKeyMap.ContainsKey(keyValue))
                    {
                        dstKeyMap.Add(keyValue, row);
                    }
                }
            }

            // 3. 키값 기반으로 Row 비교
            var sheetDiff = new ExcelSheetDiff(src, dst);
            var rowDiffList = new List<DiffResult<ExcelRow>>();

            // 추가된 Row 찾기 (dst에는 있지만 src에는 없는 키)
            foreach (var keyValue in dstKeyMap.Keys)
            {
                if (!srcKeyMap.ContainsKey(keyValue))
                {
                    var dstRow = dstKeyMap[keyValue];
                    var result = new DiffResult<ExcelRow>(null, dstRow, DiffStatus.Inserted);
                    rowDiffList.Add(result);
                }
            }

            // 삭제된 Row 찾기 (src에는 있지만 dst에는 없는 키)
            foreach (var keyValue in srcKeyMap.Keys)
            {
                if (!dstKeyMap.ContainsKey(keyValue))
                {
                    var srcRow = srcKeyMap[keyValue];
                    var result = new DiffResult<ExcelRow>(srcRow, null, DiffStatus.Deleted);
                    rowDiffList.Add(result);
                }
            }

            // 업데이트된 Row 또는 동일한 Row 찾기 (src와 dst 모두에 있는 키)
            foreach (var keyValue in srcKeyMap.Keys)
            {
                if (dstKeyMap.ContainsKey(keyValue))
                {
                    var srcRow = srcKeyMap[keyValue];
                    var dstRow = dstKeyMap[keyValue];

                    // 셀의 내용을 비교하여 변경 여부 판단
                    bool isModified = false;
                    int maxCellCount = Math.Max(srcRow.Cells.Count, dstRow.Cells.Count);

                    for (int i = 0; i < maxCellCount; i++)
                    {
                        string srcValue = i < srcRow.Cells.Count ? srcRow.Cells[i].Value : string.Empty;
                        string dstValue = i < dstRow.Cells.Count ? dstRow.Cells[i].Value : string.Empty;

                        if (srcValue != dstValue)
                        {
                            isModified = true;
                            break;
                        }
                    }

                    var status = isModified ? DiffStatus.Modified : DiffStatus.Equal;
                    var result = new DiffResult<ExcelRow>(srcRow, dstRow, status);
                    rowDiffList.Add(result);
                }
            }

            // 4. 비교 결과를 적용
            var resultArray = rowDiffList.ToArray();
            DiffCells(resultArray, sheetDiff, columnStatusMap);

            return sheetDiff;
        }

        private static ExcelSheetDiff DiffWithoutKeyColumn(ExcelSheet src, ExcelSheet dst, ExcelSheetDiffConfig config,
                                                  IEnumerable<ExcelColumn> srcColumns, IEnumerable<ExcelColumn> dstColumns,
                                                  Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var option = new DiffOption<ExcelRow>();
            option.EqualityComparer =
                new RowComparer(new HashSet<int>(columnStatusMap.Where(i => i.Value != ExcelColumnStatus.None).Select(i => i.Key)));

            foreach (var row in src.Rows.Values)
            {
                var shifted = new List<ExcelCell>();
                var index = 0;
                var queue = new Queue<ExcelCell>(row.Cells);
                while (queue.Any())
                {
                    if (columnStatusMap[index] == ExcelColumnStatus.Inserted)
                        shifted.Add(new ExcelCell(string.Empty, 0, 0));
                    else
                        shifted.Add(queue.Dequeue());

                    index++;
                }

                row.UpdateCells(shifted);
            }

            foreach (var row in dst.Rows.Values)
            {
                var shifted = new List<ExcelCell>();
                var index = 0;
                var queue = new Queue<ExcelCell>(row.Cells);
                while (queue.Any())
                {
                    if (columnStatusMap[index] == ExcelColumnStatus.Deleted)
                        shifted.Add(new ExcelCell(string.Empty, 0, 0));
                    else
                        shifted.Add(queue.Dequeue());

                    index++;
                }

                row.UpdateCells(shifted);
            }

            var resultArray = DiffUtil.OptimizedDiff(src.Rows.Values, dst.Rows.Values, option).ToArray();
            if (resultArray.Length > 10000)
            {
                var count = 0;
                var indices = Enumerable.Range(0, 100).ToList();
                foreach (var result in resultArray)
                {
                    if (result.Status != DiffStatus.Equal)
                        indices.AddRange(Enumerable.Range(Math.Max(0, count - 100), 200));

                    count++;
                }
                indices = indices.Distinct().ToList();
                resultArray = indices.Where(i => i < resultArray.Length).Select(i => resultArray[i]).ToArray();
            }

            var sheetDiff = new ExcelSheetDiff(src, dst);
            DiffCells(resultArray, sheetDiff, columnStatusMap);

            return sheetDiff;
        }

        private static Dictionary<int, ExcelColumnStatus> CreateColumnStatusMap(
            IEnumerable<ExcelColumn> srcColumns, IEnumerable<ExcelColumn> dstColumns, ExcelSheetDiffConfig config)
        {
            var option = new DiffOption<ExcelColumn>();

            if (config.SrcHeaderIndex >= 0)
            {
                option.EqualityComparer = new HeaderComparer();
                foreach (var sc in srcColumns)
                    sc.HeaderIndex = config.SrcHeaderIndex;
            }

            if (config.DstHeaderIndex >= 0)
            {
                foreach (var dc in dstColumns)
                    dc.HeaderIndex = config.DstHeaderIndex;
            }

            var results = DiffUtil.OptimizedDiff(srcColumns, dstColumns, option);
            var ret = new Dictionary<int, ExcelColumnStatus>();
            var columnIndex = 0;
            foreach (var result in results)
            {
                var status = ExcelColumnStatus.None;
                if (result.Status == DiffStatus.Deleted)
                    status = ExcelColumnStatus.Deleted;
                else if (result.Status == DiffStatus.Inserted)
                    status = ExcelColumnStatus.Inserted;

                ret.Add(columnIndex, status);
                columnIndex++;
            }

            return ret;
        }

        private IEnumerable<ExcelColumn> CreateColumns()
        {
            if (!Rows.Any())
                return Enumerable.Empty<ExcelColumn>();

            var columnCount = Rows.Max(r => r.Value.Cells.Count);
            var columns = new ExcelColumn[columnCount];
            foreach (var row in Rows)
            {
                var columnIndex = 0;
                foreach (var cell in row.Value.Cells)
                {
                    if (columns[columnIndex] == null)
                        columns[columnIndex] = new ExcelColumn();

                    columns[columnIndex].Cells.Add(cell);
                    columnIndex++;
                }
            }

            return columns.AsEnumerable();
        }

        private static void DiffCells(
            IEnumerable<DiffResult<ExcelRow>> results, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            foreach (var result in results)
            {
                switch (result.Status)
                {
                    case DiffStatus.Equal:
                        DiffCellsCaseEqual(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Modified:
                        DiffCellsCaseEqual(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Deleted:
                        DiffCellsCaseDeleted(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Inserted:
                        DiffCellsCaseInserted(result, sheetDiff, columnStatusMap);
                        break;
                }
            }
        }

        private static IEnumerable<Tuple<ExcelCell, ExcelCell>> EqualizeColumnCount(
            IEnumerable<ExcelCell> srcCells, IEnumerable<ExcelCell> dstCells, Dictionary<int, ExcelColumnStatus> columnStausMap)
        {
            var srcQueue = new Queue<ExcelCell>(srcCells);
            var dstQueue = new Queue<ExcelCell>(dstCells);
            foreach (var status in columnStausMap)
            {
                ExcelCell src = null;
                ExcelCell dst = null;

                if (srcQueue.Any()) src = srcQueue.Dequeue();
                if (dstQueue.Any()) dst = dstQueue.Dequeue();

                yield return Tuple.Create(src, dst);
            }
        }

        private static void DiffCellsCaseEqual(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var equalizedCells = EqualizeColumnCount(result.Obj1.Cells, result.Obj2.Cells, columnStatusMap);
            var columnIndex = 0;
            foreach (var pair in equalizedCells)
            {
                var srcCell = pair.Item1;
                var dstCell = pair.Item2;

                if (srcCell != null && dstCell != null)
                {
                    var status = srcCell.Value.Equals(dstCell.Value) ? ExcelCellStatus.None : ExcelCellStatus.Modified;
                    if (columnStatusMap[columnIndex] == ExcelColumnStatus.Deleted)
                        status = ExcelCellStatus.Removed;
                    else if (columnStatusMap[columnIndex] == ExcelColumnStatus.Inserted)
                        status = ExcelCellStatus.Added;

                    row.CreateCell(srcCell, dstCell, columnIndex, status);
                }
                else if (srcCell != null && dstCell == null)
                {
                    dstCell = new ExcelCell(string.Empty, srcCell.OriginalColumnIndex, srcCell.OriginalColumnIndex);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.Removed);
                }
                else if (srcCell == null && dstCell != null)
                {
                    srcCell = new ExcelCell(string.Empty, dstCell.OriginalColumnIndex, dstCell.OriginalColumnIndex);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.Added);
                }
                else
                {
                    srcCell = new ExcelCell(string.Empty, 0, 0);
                    dstCell = new ExcelCell(string.Empty, 0, 0);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.None);
                }

                columnIndex++;
            }
        }

        private static void DiffCellsCaseDeleted(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var columnIndex = 0;
            foreach (var cell1 in result.Obj1.Cells)
            {
                var cell2 = new ExcelCell(string.Empty, cell1.OriginalColumnIndex, cell1.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex, ExcelCellStatus.Removed);

                columnIndex++;
            }
        }

        private static void DiffCellsCaseInserted(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var columnIndex = 0;
            foreach (var cell2 in result.Obj2.Cells)
            {
                var cell1 = new ExcelCell(string.Empty, cell2.OriginalColumnIndex, cell2.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex, ExcelCellStatus.Added);

                columnIndex++;
            }
        }
    }
}
