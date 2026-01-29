namespace OpenglAvaloniaTest.Commands;

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
}
