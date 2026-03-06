namespace OpenglAvaloniaTest.Commands;

[System.Flags]
public enum CommandInfo
{
    None,
    KeyDown = 1 << 0,
    KeyUp = 1 << 1,
    MouseMove = 1 << 2,
    MouseDown = 1 << 3,
    MouseUp = 1 << 4 ,
    Initialization = 1 << 5,
    MouseEvent = MouseMove | MouseDown | MouseUp,
    KeyInfo = KeyDown | KeyUp,
}