namespace ExcelMerge.Core;

public class ExcelSheetDiffConfig
{
    public int SrcSheetIndex { get; set; }
    public int DstSheetIndex { get; set; }
    public int SrcHeaderIndex { get; set; }
    public int DstHeaderIndex { get; set; }
    public bool UseKeyColumn { get; set; }
    public string KeyColumnName { get; set; }
}
