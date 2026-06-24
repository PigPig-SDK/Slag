using Avalonia.Input;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Data;
using UI.Commands;
using UI.Views;


namespace UI.ViewModels;

public class InputManager
{
    public static InputManager Singleton { get; set; } = new();

    public UserControlMode UserControlMode { get; private set; } = UserControlMode.None;

    private Dictionary<Key, CommandTypes> _keyBindFactory = new(){
    {Key.G, CommandTypes.Move},
    {Key.R, CommandTypes.Rotate},
    {Key.S, CommandTypes.Scale},
    {Key.E, CommandTypes.Extrude},
    {Key.F, CommandTypes.Flip},
    {Key.Delete, CommandTypes.Delete},
};

    public DateTime LastRightClick { get; private set; }
    public DateTime LastLeftClick { get; private set; }

    //TOTO: REFACTOR THIS.
    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep(new CommandArguments(e, null, CommandInfo.KeyDown))) return;
        CommandArguments cmdInfo = new(e, null, CommandInfo.Initialization | CommandInfo.KeyDown);
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
            case Key.D1:
                SelectionManager.Instance.CurrentSelectionMode = SelectionMode.Vertex;
                break;
            case Key.D2:
                SelectionManager.Instance.CurrentSelectionMode = SelectionMode.Edge;
                break;
            case Key.D3:
                SelectionManager.Instance.CurrentSelectionMode = SelectionMode.Face;
                break;
            case Key.D4:
                SelectionManager.Instance.CurrentSelectionMode = SelectionMode.Mesh;
                break;
            case Key.S:
                if (!UserControlMode.HasFlag(UserControlMode.Ctrl)) goto default;//Let command handle it... We only care about CTRL+S
                if(!OBJFile.TrySaveOBJ())
                {
                    MainWindow.Instance.OnFileSave(null, e);
                }
                break;
            default:
                {
                    if(_keyBindFactory.TryGetValue(e.Key, out CommandTypes commandTypes))
                    {
                        if (!CommandLookup.CommandFactory.TryGetValue(commandTypes, out Func<CommandArguments, ICommand>? factory)) return;

                        if (factory is null) return;

                        ICommand command = factory.Invoke(cmdInfo);
                        CommandInvoker.Singleton?.RunCommand(command);
                    }
                    break;
                }
        }
    }

    public void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep(new CommandArguments(e, null, CommandInfo.KeyUp))) return;
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
        if (CommandInvoker.Singleton.ExecuteCommandStep(new CommandArguments(null, e, CommandInfo.MouseUp))) return;

        Camera.Instance.OnMouseUp(sender, e);
        
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
    {
        if (CommandInvoker.Singleton.ExecuteCommandStep(new CommandArguments(null, e, CommandInfo.MouseMove))) return;

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

        if (CommandInvoker.Singleton.ExecuteCommandStep(new CommandArguments(null, e, CommandInfo.MouseDown))) return;

        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = e.GetScreenPos();
            SelectionManager.Instance.CheckForSelection(screenLocation, false);
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }
}
