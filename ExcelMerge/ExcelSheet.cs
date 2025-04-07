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
            Rows = [];
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

            foreach (ExcelRow row in rows)
                sheet.Rows.Add(row.Index, row);

            return sheet;
        }

        public static ExcelSheetDiff Diff(ExcelSheet src, ExcelSheet dst, ExcelSheetDiffConfig config)
        {
            IEnumerable<ExcelColumn> srcColumns = src.CreateColumns();
            IEnumerable<ExcelColumn> dstColumns = dst.CreateColumns();
            Dictionary<int, ExcelColumnStatus> columnStatusMap = CreateColumnStatusMap(srcColumns, dstColumns, config);

            if (config.UseKeyColumn && !string.IsNullOrEmpty(config.KeyColumnName))
                return DiffWithKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
            else
                return DiffWithoutKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
        }

        /// <summary>
        /// Column 수 똑같이 맞추기
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="columnStatusMap"></param>
        private static void FitLayout(ExcelSheet src, ExcelSheet dst, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            foreach (var row in src.Rows.Values)
            {
                var shifted = new List<ExcelCell>();
                var index = 0;
                var queue = new Queue<ExcelCell>(row.Cells);
                while (queue.Count != 0)
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
                while (queue.Count != 0)
                {
                    if (columnStatusMap[index] == ExcelColumnStatus.Deleted)
                        shifted.Add(new ExcelCell(string.Empty, 0, 0));
                    else
                        shifted.Add(queue.Dequeue());

                    index++;
                }

                row.UpdateCells(shifted);
            }
        }

        /// <summary>
        /// 최적화를 위한 시트 좌표 생성
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="columnStatusMap"></param>
        private static DiffResult<ExcelRow>[] OptimizeSheetLayout(ExcelSheet src, ExcelSheet dst, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var option = new DiffOption<ExcelRow>
            {
                EqualityComparer = new RowComparer(new HashSet<int>(columnStatusMap.Where(i => i.Value != ExcelColumnStatus.None).Select(i => i.Key)))
            };
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

            return resultArray;
        }

        private static ExcelSheetDiff DiffWithKeyColumn(
            ExcelSheet src,
            ExcelSheet dst,
            ExcelSheetDiffConfig config,
            IEnumerable<ExcelColumn> srcColumns,
            IEnumerable<ExcelColumn> dstColumns,
            Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            // 1. 키 컬럼에 해당하는 컬럼 인덱스 찾기
            int? srcKeyColumnIndex = null;
            int? dstKeyColumnIndex = null;

            #region 키 컬럼 이름이 있는 칸을 헤더에서 찾기
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
            #endregion

            #region 키 컬럼을 찾지 못한 경우 기본 형태로 비교
            if (!srcKeyColumnIndex.HasValue || !dstKeyColumnIndex.HasValue)
                return DiffWithoutKeyColumn(src, dst, config, srcColumns, dstColumns, columnStatusMap);
            #endregion

            // Key Column 모드에서는 키값을 먼저 체크한 뒤 레이아웃을 맞추는 것이 논리적으로 맞다.
            FitLayout(src, dst, columnStatusMap);
            DiffResult<ExcelRow>[] resultArray = OptimizeSheetLayout(src, dst, columnStatusMap);

            // 2. 키값에 따라 맵 생성
            Dictionary<string, ExcelRow> srcKeyMap = [];
            Dictionary<string, ExcelRow> dstKeyMap = [];

            #region 소스와 대상 Row를 키 값에 따라 보관
            foreach (ExcelRow row in src.Rows.Values)
            {
                string keyValue = string.Empty;
                if (srcKeyColumnIndex.Value < row.Cells.Count)
                    keyValue = row.Cells[srcKeyColumnIndex.Value].Value;

                string mapKey = !string.IsNullOrEmpty(keyValue) ? keyValue : $"__NO_KEY_SRC_{row.Index}";
                srcKeyMap.TryAdd(mapKey, row);
            }

            foreach (ExcelRow row in dst.Rows.Values)
            {
                string keyValue = string.Empty;
                if (dstKeyColumnIndex.Value < row.Cells.Count)
                    keyValue = row.Cells[dstKeyColumnIndex.Value].Value;

                // 키 값이 없어도 행 자체는 매핑 (빈 키 또는 인덱스 기반 키 사용)
                string mapKey = !string.IsNullOrEmpty(keyValue) ? keyValue : $"__NO_KEY_DST_{row.Index}";
                dstKeyMap.TryAdd(mapKey, row);
            }
            #endregion

            // 3. 키값 기반으로 Row 비교
            ExcelSheetDiff sheetDiff = new(src, dst);
            List<DiffResult<ExcelRow>> rowDiffList = [];

            // 삭제된 행: 소스에만 있는 키
            foreach (KeyValuePair<string, ExcelRow> entry in srcKeyMap)
            {
                string key = entry.Key;
                ExcelRow srcRow = entry.Value;

                // 대상에 같은 키가 없으면 "삭제됨"
                if (!dstKeyMap.TryGetValue(key, out ExcelRow dstRow) || key.StartsWith("__NO_KEY_SRC_"))
                    rowDiffList.Add(new DiffResult<ExcelRow>(srcRow, null, DiffStatus.Deleted));
                else
                {
                    bool isModified = !CompareRowContents(srcRow, dstRow);
                    var status = isModified ? DiffStatus.Modified : DiffStatus.Equal;
                    rowDiffList.Add(new DiffResult<ExcelRow>(srcRow, dstRow, status));

                    // 처리된 대상 키 제거
                    dstKeyMap.Remove(key);
                }
            }

            // 추가된 행: 대상에만 있는 남은 키
            foreach (KeyValuePair<string, ExcelRow> entry in dstKeyMap)
                rowDiffList.Add(new DiffResult<ExcelRow>(null, entry.Value, DiffStatus.Inserted));

            // 업데이트된 Row 또는 동일한 Row 찾기 (src와 dst 모두에 있는 키)
            foreach (string keyValue in srcKeyMap.Keys)
            {
                if (!dstKeyMap.TryGetValue(keyValue, out ExcelRow dstRow))
                    continue;

                ExcelRow srcRow = srcKeyMap[keyValue];

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

            // 4. 비교 결과를 적용
            // 행 인덱스 기준으로 정렬하여 원래 순서 유지
            var orderedResults = rowDiffList
                .OrderBy(r => r.Obj1 != null ? r.Obj1.Index : int.MaxValue)
                .ThenBy(r => r.Obj2 != null ? r.Obj2.Index : int.MaxValue)
                .ToArray();
            DiffCells(orderedResults, sheetDiff, columnStatusMap);

            return sheetDiff;
        }

        private static bool CompareRowContents(ExcelRow row1, ExcelRow row2)
        {
            int maxCellCount = Math.Max(row1.Cells.Count, row2.Cells.Count);

            for (int i = 0; i < maxCellCount; i++)
            {
                string value1 = i < row1.Cells.Count ? row1.Cells[i].Value : string.Empty;
                string value2 = i < row2.Cells.Count ? row2.Cells[i].Value : string.Empty;

                if (value1 != value2)
                    return false;
            }

            return true;
        }

        private static ExcelSheetDiff DiffWithoutKeyColumn(
            ExcelSheet src,
            ExcelSheet dst,
            ExcelSheetDiffConfig config,
            IEnumerable<ExcelColumn> srcColumns,
            IEnumerable<ExcelColumn> dstColumns,
            Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            FitLayout(src, dst, columnStatusMap);
            DiffResult<ExcelRow>[] resultArray = OptimizeSheetLayout(src, dst, columnStatusMap);
            ExcelSheetDiff sheetDiff = new(src, dst);
            DiffCells(resultArray, sheetDiff, columnStatusMap);

            return sheetDiff;
        }

        private static Dictionary<int, ExcelColumnStatus> CreateColumnStatusMap(
            IEnumerable<ExcelColumn> srcColumns,
            IEnumerable<ExcelColumn> dstColumns,
            ExcelSheetDiffConfig config)
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
            if (Rows.Count == 0)
                return [];

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
            IEnumerable<DiffResult<ExcelRow>> results,
            ExcelSheetDiff sheetDiff,
            Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            foreach (DiffResult<ExcelRow> result in results)
            {
                switch (result.Status)
                {
                    case DiffStatus.Equal:
                    case DiffStatus.Modified:
                        DiffCellsCaseEqual(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Deleted:
                        DiffCellsCaseDeleted(result, sheetDiff);
                        break;
                    case DiffStatus.Inserted:
                        DiffCellsCaseInserted(result, sheetDiff);
                        break;
                }
            }
        }

        private static IEnumerable<Tuple<ExcelCell, ExcelCell>> EqualizeColumnCount(
            IEnumerable<ExcelCell> srcCells,
            IEnumerable<ExcelCell> dstCells,
            Dictionary<int, ExcelColumnStatus> columnStausMap)
        {
            var srcQueue = new Queue<ExcelCell>(srcCells);
            var dstQueue = new Queue<ExcelCell>(dstCells);
            foreach (var status in columnStausMap)
            {
                ExcelCell src = null;
                ExcelCell dst = null;

                if (srcQueue.Count != 0) src = srcQueue.Dequeue();
                if (dstQueue.Count != 0) dst = dstQueue.Dequeue();

                yield return Tuple.Create(src, dst);
            }
        }

        private static void DiffCellsCaseEqual(
            DiffResult<ExcelRow> result,
            ExcelSheetDiff sheetDiff,
            Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            ExcelRowDiff row = sheetDiff.CreateRow();
            IEnumerable<Tuple<ExcelCell, ExcelCell>> equalizedCells = EqualizeColumnCount(result.Obj1.Cells, result.Obj2.Cells, columnStatusMap);
            int columnIndex = 0;
            foreach (Tuple<ExcelCell, ExcelCell> pair in equalizedCells)
            {
                ExcelCell srcCell = pair.Item1;
                ExcelCell dstCell = pair.Item2;

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

        private static void DiffCellsCaseDeleted(DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff)
        {
            ExcelRowDiff row = sheetDiff.CreateRow();
            int columnIndex = 0;
            foreach (ExcelCell cell1 in result.Obj1.Cells)
            {
                ExcelCell cell2 = new(string.Empty, cell1.OriginalColumnIndex, cell1.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex++, ExcelCellStatus.Removed);
            }
        }

        private static void DiffCellsCaseInserted(DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff)
        {
            ExcelRowDiff row = sheetDiff.CreateRow();
            int columnIndex = 0;
            foreach (ExcelCell cell2 in result.Obj2.Cells)
            {
                ExcelCell cell1 = new(string.Empty, cell2.OriginalColumnIndex, cell2.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex++, ExcelCellStatus.Added);
            }
        }
    }
}
