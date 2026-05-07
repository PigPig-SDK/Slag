using Avalonia.Input;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Data;
using UI.Commands;


namespace UI.ViewModels;

public class InputManager
{
    public static InputManager Singleton { get; set; } = new();

    public UserControlMode UserControlMode { get; private set; } = UserControlMode.None;

    private Dictionary<Key, Type> _keyBinds = new(){
        {Key.G, typeof(MoveCommand)},
        {Key.R, typeof(RotateCommand)},
        {Key.S, typeof(ScaleCommand)},
        {Key.E, typeof(ExtrudeCommand)},
        {Key.F, typeof(FlipCommand)},
        {Key.Delete, typeof(DeleteCommand)},
    };

    public DateTime LastRightClick { get; private set; }
    public DateTime LastLeftClick { get; private set; }

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
            case Key.V:
                {
                    if (UserControlMode.HasFlag(UserControlMode.Ctrl) 
                        && SelectionManager.Instance.CurrentSelectionMode == SelectionMode.Mesh)
                        ClipBoard.Instance.Paste();
                    break;
                }
            case Key.C:
                {
                    if (UserControlMode.HasFlag(UserControlMode.Ctrl)
                        && SelectionManager.Instance.CurrentSelectionMode == SelectionMode.Mesh)
                        ClipBoard.Instance.Copy(SelectionManager.Instance.CurrentBroadModels);
                    break;
                }
            case Key.A:
                {
                    if (!UserControlMode.HasFlag(UserControlMode.Ctrl)) return;

                    SelectionManager.Instance.SelectAll();
                    break;
                }
            default:
                {
                    if(_keyBinds.TryGetValue(e.Key, out Type? value))
                    {
                        if (value is null) return;
                        if (Activator.CreateInstance(value) is not ICommand command) return;

                        CommandInvoker.Singleton?.RunCommand(command, cmdInfo);
                    }
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

        Camera.Instance.OnMouseUp(sender, e);
        
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseMove))) return;

        var properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if(properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = e.GetScreenPos();
            SelectionManager.Instance.CheckForSelection(screenLocation, true);
            return;
        }

        Camera.Instance.OnPointerMove(sender, e);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(GLControl.Instance).Properties;

        if (properties.IsLeftButtonPressed)
            LastLeftClick = DateTime.Now;
        if (properties.IsRightButtonPressed)
            LastRightClick = DateTime.Now;

        if (CommandInvoker.Singleton.ExecuteCommandStep((null, e, CommandInfo.MouseDown))) return;

        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = e.GetScreenPos();
            SelectionManager.Instance.CheckForSelection(screenLocation, false);
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }
}
