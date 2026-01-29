using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Reflection;

namespace OpenglAvaloniaTest.Commands;

public class MoveCommand : ICommand
{
    public ICommand? Next { get; set; }

    
    public Vector3 MoveDir = new Vector3(0,1,0);
    public Vector3 StartPos = new Vector3(0,0,0);
    private Vector2? _MouseStartPos = null;


    private CommandState Initialize()
    {
        Vector3? pos = SelectionManager.Instance.CurrentModel!.GetComponent<SelectionComponent>()?.GetCenter();
        if(pos.HasValue)
        {
            StartPos = pos.Value;
            Console.WriteLine($"MoveCommand initialized at position {StartPos}");
        }
        else
        {
            Console.WriteLine("MoveCommand initialization failed: Could not get selection center.");
            return CommandState.Finished;
        }

        Console.WriteLine("Executing MoveCommand");
        return CommandState.Idle;//Continue the command.
    }

    private void MoveSelection(float ammount)
    {
        Model? model = SelectionManager.Instance.CurrentModel;
        if (model == null) throw new Exception("No current model in MoveCommand.MoveSelection");

        SelectionComponent? selection = model.GetComponent<SelectionComponent>();
        if(selection == null) throw new Exception($"No selection component {nameof(selection)}!");

        foreach(uint index in selection.IterateSelection())
        {
            model.TryMoveVertex(index, Vector3.Zero);
        }
        model.UpdateAllComponents(UpdateType.Locational, null);
    }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        //No model, no command.
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Finished;

        if(args.info.HasFlag(CommandInfo.Initialization))
            return Initialize();

        //Is a mouse input
        if ((args.info & CommandInfo.KeyInfo) == 0)
        {
            var mouseInfo = args.mouseEvent!.GetPosition(GLControl.Instance);
            if (_MouseStartPos == null) _MouseStartPos = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y);

            Vector2 mouseDelta = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y) - _MouseStartPos.Value;
            MoveSelection(mouseDelta.Length);
        }

            //Don't register keyup events
        if (args.info.HasFlag(CommandInfo.KeyUp)) return CommandState.Idle;

        switch(args.keyEvent?.Key)
        {
            case Key.G:
                Console.WriteLine("Move confirmed");
                return CommandState.Finished;
            case Key.Escape:
                Console.WriteLine("Move declined");
                return CommandState.Finished;
        }

        return CommandState.Idle;
    }
}
