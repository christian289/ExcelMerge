namespace ExcelMerge.GUI.ValueConverters;

public class MultiEmptyStringConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null)
            return false;

        if (values.Length == 0)
            return false;

        var ret = true;
        foreach (var value in values)
        {
            ret &= !string.IsNullOrEmpty(value?.ToString());
        }

        return ret;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
