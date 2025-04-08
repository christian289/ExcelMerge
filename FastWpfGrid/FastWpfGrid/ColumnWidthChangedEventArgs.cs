namespace FastWpfGrid;

public class ColumnWidthChangedEventArgs : EventArgs
{
    public int Column { get; }
    public int NewWidth { get; }

    public ColumnWidthChangedEventArgs(int column, int newWidth)
    {
        Column = column;
        NewWidth = newWidth;
    }
}
