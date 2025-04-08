namespace ExcelMerge.GUI.Behaviors;

public sealed class DragAcceptDescription
{
    public event Action<DragEventArgs> DragOver;

    public void OnDragOver(DragEventArgs dragEventArgs)
    {
        DragOver?.Invoke(dragEventArgs);
    }

    public event Action<DragEventArgs> DragDrop;

    public void OnDrop(DragEventArgs dragEventArgs)
    {
        DragDrop?.Invoke(dragEventArgs);
    }
}
