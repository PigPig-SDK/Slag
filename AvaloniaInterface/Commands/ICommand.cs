using Avalonia.Input;

namespace OpenglAvaloniaTest.Commands;

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
    CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args);
}
