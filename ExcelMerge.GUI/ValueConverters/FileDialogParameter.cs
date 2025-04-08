namespace ExcelMerge.GUI.ValueConverters;

public class FileDialogParameter
{
    public object Obj { get; private set; }
    public PropertyInfo PropertyInfo { get; private set; }
    public string Title { get; set; } = "Open File";

    public FileDialogParameter(object obj, PropertyInfo propertyInfo)
    {
        Obj = obj;
        PropertyInfo = propertyInfo;
    }
}
