using Avalonia.Input;

namespace UI.Commands;


public interface ICommand
{
    /// <summary>
    /// The following command to execute after this one
    /// </summary>
    public ICommand? Next { get; set; }
    /// <summary>
    /// Executes the command
    /// </summary>
    /// <returns>The command state after execution</returns>
    public CommandState Execute(CommandArguments args);

    public string Name { get; }

    public string Description { get; }

    public bool DisplayToolText { get; }

    public bool AllowInMeshMode { get; }

    public string IconSource { get; }

    public void Undo();

    public void Redo();
}
