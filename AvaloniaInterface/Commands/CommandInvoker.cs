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

    private FixedSizeStack<ICommand> _UndoQueue = new(40);
    private FixedSizeStack<ICommand> _RedoQueue = new(40);

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
            _UndoQueue.Push(CurrentCommand);
            _RedoQueue.Clear();
            Console.WriteLine(_UndoQueue);
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
        if(_RedoQueue.Count <= 0) return false;
        ICommand command = _RedoQueue.Pop();
        command.Redo();
        _UndoQueue.Push(command);
        return true;
    }
    public bool ExecuteUndo()
    {
        if(_UndoQueue.Count <= 0) return false;
        ICommand command = _UndoQueue.Pop();
        command.Undo();
        _RedoQueue.Push(command);
        return true;
    }
}
