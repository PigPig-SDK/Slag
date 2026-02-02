using Avalonia.Input;
using OpenglAvaloniaTest.Commands;
using OpenTK.Mathematics;


namespace OpenglAvaloniaTest.ViewModels;

public class InputManager
{
    public static InputManager Singleton = new InputManager();

    public UserControlMode UserControlMode { get; private set; } = UserControlMode.None;

    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((e, null, CommandInfo.KeyDown))) return;

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
                    CommandInvoker.Singleton.RunCommand(new MoveCommand(), (e, null, CommandInfo.Initialization | CommandInfo.KeyDown));
                    break;
                }
            case Key.S:
                {
                    CommandInvoker.Singleton.RunCommand(new ScaleCommand(), (e, null, CommandInfo.Initialization | CommandInfo.KeyDown));
                    break;
                }
            case Key.Delete:
                {
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
            Vector2 screenLocation = new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
            SelectionManager.Instance.CheckForSelection(screenLocation);
            return;
        }

        Camera.Instance?.OnPointerMove(sender, e);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseDown))) return;

        var properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
            SelectionManager.Instance.CheckForSelection(screenLocation);
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }
}
