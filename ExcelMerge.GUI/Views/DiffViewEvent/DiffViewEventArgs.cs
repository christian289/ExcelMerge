namespace ExcelMerge.GUI.Views.DiffViewEvent
{
    enum TargetType
    {
        All,
        First,
    }

    class DiffViewEventArgs<T> : EventArgs
    {
        public T Sender { get; }
        public IUnityContainer Container { get; }
        public TargetType TargetType { get; }

        public DiffViewEventArgs(T sender, IUnityContainer container, TargetType targetType = TargetType.All)
        {
            Sender = sender;
            Container = container;
            TargetType = targetType;
        }
    }
}
