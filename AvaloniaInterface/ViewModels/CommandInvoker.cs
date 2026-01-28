using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

public class CommandInvoker
{
    public static CommandInvoker Singleton = new();

    public ICommand? CurrentCommand { get; set; }

    public void RunCommand(ICommand command, (KeyEventArgs key, PointerEventArgs mouse) args)
    {
        if(CurrentCommand != null)
        {
            Console.WriteLine("A command is already running!");  
            return;
        }
         
        CurrentCommand = command;
        ExecuteCommandStep(args);
    }

    public void ExecuteCommandStep((KeyEventArgs key, PointerEventArgs mouse) args)
    {
        if(CurrentCommand == null)
            return;
        CommandState commandState = CurrentCommand.Execute();
        if (commandState != CommandState.Idle)
        {
            CurrentCommand = CurrentCommand.Next;
            if(CurrentCommand != null && commandState == CommandState.Continue)// Continue immediately to next command
            {
                ExecuteCommandStep(args);
            }
        }   
    }
}
