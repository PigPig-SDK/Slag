namespace UI.Commands;

public enum CommandState
{
    /// <summary>
    /// Remain on this command
    /// </summary>
    Idle,
    /// <summary>
    /// Continue to the next command immediately
    /// </summary>
    Continue,
    /// <summary>
    /// Finish this command and wait for the next trigger to continue
    /// </summary>
    Finished,
    /// <summary>
    /// Next command might take user input...
    /// </summary>
    Yield,
    /// <summary>
    /// Discard the current command, do not append to undo/redo stack.
    /// </summary>
    Discard,
}
