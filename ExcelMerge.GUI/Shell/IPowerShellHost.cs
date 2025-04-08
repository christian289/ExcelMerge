namespace ExcelMerge.GUI.Shell;

public interface IPowerShellHost
{
	ReadOnlyObservableCollection<IPowerShellInvocation> Invocations { get; }
}
