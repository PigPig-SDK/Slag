using Avalonia;
using Avalonia.Input;
using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

[Flags]
public enum UserControlMode
{
    None = 0,
    Shift = 1 << 0,
    Ctrl = 1 << 1,
    Alt = 1 << 2,
}

public class InputManager
{
    public static InputManager Singleton = new InputManager();

    public UserControlMode UserControlMode { get; private set; } = UserControlMode.None;

    public void OnMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        Camera.Instance?.OnMouseUp(sender, e);
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
    {
        Camera.Instance?.OnPointerMove(sender, e);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
            SelectionManager.Instance.CheckForSelection(screenLocation);
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }

    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl: case Key.RightCtrl:
                {
                    UserControlMode |= UserControlMode.Ctrl;
                    break;
                }
            case Key.Delete:
                {

                    break;
                }
        }
    }

    internal void OnKeyUp(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl:case Key.RightCtrl:
                {
                    UserControlMode &= ~UserControlMode.Ctrl;
                    break;
                }
        }

    }
}
