using Avalonia.Input;
using UI.Commands;
using OpenTK.Mathematics;


namespace UI.ViewModels;

public class InputManager
{
    public static InputManager Singleton = new InputManager();

    public UserControlMode UserControlMode { get; private set; } = UserControlMode.None;

    //TOTO: REFACTOR THIS.
    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((e, null, CommandInfo.KeyDown))) return;
        (KeyEventArgs?, PointerEventArgs?, CommandInfo) cmdInfo = (e, null, CommandInfo.Initialization | CommandInfo.KeyDown);
        switch (e.Key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                {
                    UserControlMode |= UserControlMode.Ctrl;
                    break;
                }
            case Key.G:
                {
                    CommandInvoker.Singleton?.RunCommand(new MoveCommand(), cmdInfo);
                    break;
                }
            case Key.R:
                {
                    CommandInvoker.Singleton?.RunCommand(new RotateCommand(), cmdInfo);
                    break;
                }
            case Key.S:
                {
                    CommandInvoker.Singleton?.RunCommand(new ScaleCommand(), cmdInfo);
                    break;
                }
            case Key.E:
                {
                    CommandInvoker.Singleton?.RunCommand(new ExtrudeCommand(), cmdInfo);
                    break;
                }
            case Key.Delete:
                {
                    CommandInvoker.Singleton?.RunCommand(new DeleteCommand(), cmdInfo);
                    break;
                }
            case Key.Z:
                {
                    if(UserControlMode.HasFlag(UserControlMode.Ctrl))
                        CommandInvoker.Singleton.ExecuteUndo();
                    break;
                }
            case Key.Y:
                {
                    if (UserControlMode.HasFlag(UserControlMode.Ctrl))
                        CommandInvoker.Singleton.ExecuteRedo();
                    break;
                }
        }
    }

    public void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((e, null, CommandInfo.KeyUp))) return;
        switch (e.Key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                {
                    UserControlMode &= ~UserControlMode.Ctrl;
                    break;
                }
        }

    }

    public void OnMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseUp))) return;

        Camera.Instance?.OnMouseUp(sender, e);
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseMove))) return;

        var properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if(properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = e.GetScreenPos(GLControl.Instance!);
            SelectionManager.Instance.CheckForSelection(screenLocation, true);
            return;
        }

        Camera.Instance?.OnPointerMove(sender, e);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseDown))) return;

        PointerPointProperties properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = e.GetScreenPos(GLControl.Instance!);
            SelectionManager.Instance.CheckForSelection(screenLocation, false);
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }
}
