using Avalonia.Input;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.Commands;

public class CommandInvoker
{
    public static CommandInvoker Singleton = new();

    public ICommand? CurrentCommand { get; set; }

    private FixedSizeStack<ICommand> _undoQueue = new(40);
    private FixedSizeStack<ICommand> _redoQueue = new(40);

    public void RunCommand(ICommand command, (KeyEventArgs? key, PointerEventArgs? mouse, CommandInfo info) args)
    {
        if(CurrentCommand != null)
        {
            Console.WriteLine("A command is already running!");  
            return;
        }
         
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
            return true;

        if (commandState != CommandState.Idle)
        {
            _undoQueue.Push(CurrentCommand);
            _redoQueue.Clear();
            Console.WriteLine(_undoQueue);
            CurrentCommand = CurrentCommand.Next;
            if(CurrentCommand != null && commandState == CommandState.Continue)// Continue immediately to next command
            {
                ExecuteCommandStep(args);
            }
            else if(commandState == CommandState.Finished)
            {
                CurrentCommand = null;
            }
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
}
