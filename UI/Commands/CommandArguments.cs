using Avalonia.Input;

namespace UI.Commands;


public class CommandArguments
{
    public KeyEventArgs? KeyPressEvent { get; set; }
    public PointerEventArgs? MouseEvent { get; set; }
    public CommandInfo CommandInfo { get; set; }

    public CommandArguments(KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info)
    {
        this.KeyPressEvent = keyEvent;
        this.MouseEvent = mouseEvent;
        this.CommandInfo = info;
    }
}
