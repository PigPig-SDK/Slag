using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenglAvaloniaTest.Commands;

public class MoveCommand : ICommand
{
    public ICommand? Next { get; set; }

    private const float _MoveDistanceScale = 0.01f;
    public Vector3 MoveDir = Vector3.Zero;
    public Vector3 StartPos = new Vector3(0,0,0);
    private Vector2? _MouseStartPos = null;

    private Dictionary<uint, Vector3> _StartingPosition = [];

    private CommandState Initialize()
    {
        Model? activeModel = SelectionManager.Instance.CurrentModel;
        if (activeModel == null) return CommandState.Finished;//Cannot execute command

        SelectionComponent? selection = activeModel.GetComponent<SelectionComponent>();
        if(selection is null) return CommandState.Finished;

        StartPos = selection.GetCenter();
        int count = 0;
        foreach(uint index in selection.SelectionIndicies())
        {
            Vertex vert = activeModel.GetVertex(index);
            count++;
            _StartingPosition[index] = vert.Position;
            MoveDir += vert.Position.Normalized();
        }
        MoveDir /= count;
        Console.WriteLine($"Move dir : {MoveDir}");
        return CommandState.Idle;//Continue the command.
    }

    private void MoveSelection(float ammount)
    {
        Model? model = SelectionManager.Instance.CurrentModel;
        if (model == null) throw new Exception("No current model in MoveCommand.MoveSelection");

        SelectionComponent? selection = model.GetComponent<SelectionComponent>();
        if(selection == null) throw new Exception($"No selection component {nameof(selection)}!");

        Vertex[] vertices = model.Verticies.BackingField();

        foreach (uint index in selection.SelectionIndicies())
        {
            vertices[index].Position = _StartingPosition[index] + MoveDir * ammount * _MoveDistanceScale;
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
