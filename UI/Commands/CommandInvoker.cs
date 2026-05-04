using Avalonia.Input;
using System;
using System.Linq;
using Core;
using UI.ViewModels;

namespace UI.Commands;

public class CommandInvoker
{
    public static CommandInvoker Singleton { get; private set; } = new();

    public ICommand? CurrentCommand { get; set; }

    /// <summary>
    /// If NULL, than no command is being executed
    /// </summary>
    public event Action<ICommand?>? CommandTextUpdated;

    private readonly FixedSizeStack<ICommand> _undoQueue = new(40);
    private readonly FixedSizeStack<ICommand> _redoQueue = new(40);

    public void RunCommand(ICommand command, (KeyEventArgs? key, PointerEventArgs? mouse, CommandInfo info) args)
    {
        if(CurrentCommand != null)
        {
            return;
        }

        CommandTextUpdated?.Invoke(command);
        CurrentCommand = command;
        ExecuteCommandStep(args);
    }
    /// <param name="args">The user input to process</param>
    /// <returns>True if the command was processed, False if no command is active</returns>
    public bool ExecuteCommandStep((KeyEventArgs? key, PointerEventArgs? mouse, CommandInfo info) args)
    {
        if(CurrentCommand == null)
            return false;

        CommandState commandState = CurrentCommand.Execute(args);

        if (commandState == CommandState.Discard)
        {
            CommandTextUpdated?.Invoke(null);//No command now...
            CurrentCommand = null;
            return true;
        }

        if (commandState != CommandState.Idle ||
            !CurrentCommand.AllowInMeshMode && SelectionManager.Instance.CurrentSelectionMode == SelectionMode.Mesh)
        {
            _undoQueue.Push(CurrentCommand);
            _redoQueue.Clear();
            CurrentCommand = CurrentCommand.Next;
            if (CurrentCommand != null && commandState == CommandState.Continue)// Continue immediately to next command
            {
                ExecuteCommandStep(args);
            }
            else if(commandState == CommandState.Finished)
            {
                CurrentCommand = null;
            }
            CommandTextUpdated?.Invoke(CurrentCommand);//No command now...
        }
        return true;
    }
    public bool ExecuteRedo()
    {
        if(_redoQueue.Count <= 0) return false;
        ICommand command = _redoQueue.Pop();
        command.Redo();
        _undoQueue.Push(command);
        return true;
    }
    public bool ExecuteUndo()
    {
        if(_undoQueue.Count <= 0) return false;
        ICommand command = _undoQueue.Pop();
        command.Undo();
        _redoQueue.Push(command);
        return true;
    }

    public void UpdateCommandInfo(ICommand cmd)
    {
        CommandTextUpdated?.Invoke(cmd);
    }
}
