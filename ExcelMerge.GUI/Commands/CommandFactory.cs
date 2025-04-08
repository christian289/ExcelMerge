namespace ExcelMerge.GUI.Commands;

public static class CommandFactory
{
    public static ICommand Create(CommandLineOption option)
    {
        return option.MainCommand switch
        {
            CommandType.None => new DiffCommand(option),
            CommandType.Diff => new DiffCommand(option),
            _ => throw new Exceptions.ExcelMergeException(true, $"{option.MainCommand} is unkown command"),
        };
    }
}
