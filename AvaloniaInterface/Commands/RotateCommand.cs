using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenglAvaloniaTest.Commands;

public class RotateCommand : ICommand
{
    public ICommand? Next { get; set; }
    private List<uint> _selectedIndicies = [];
    private Vector3 selectionCenter { get; set; }
    private Vector2? _mouseStart;
    private Dictionary<uint, Vector4> _startingPosition = [];
    private float _totalRotation;
    private float? _initialRotation = null;

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;
        
        if(args.info.HasFlag(CommandInfo.Initialization)) return Initialize();

        if(args.info == CommandInfo.MouseEvent)
        {
            var mouseInfo = args.mouseEvent!.GetPosition(GLControl.Instance);
            Vector2 mousePos = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y);
            _mouseStart ??= mousePos;
            Vector2 distanceVector = mousePos - _mouseStart.Value;
            if(distanceVector.LengthSquared == 0) return CommandState.Idle;//Cannot compute right now.
            float angle = MathF.Atan2(distanceVector.Y, distanceVector.X);
            _initialRotation ??= angle;
            _totalRotation = angle - _initialRotation.Value;
            Rotate(_totalRotation);
        }

        return CommandState.Continue;
    }

    private void Rotate(float ammount)
    {
        Console.WriteLine(ammount);
    }

    private CommandState Initialize()
    {
        if (Camera.Instance == null) throw new InvalidOperationException($"No camera in {nameof(MoveCommand)} {nameof(Initialize)}");
        if (GLControl.Instance == null) throw new InvalidOperationException($"No such {nameof(GLControl.Instance)}");

        Model? activeModel = SelectionManager.Instance.CurrentModel;
        if (activeModel == null) return CommandState.Discard;//Cannot execute command

        SelectionComponent? selection = activeModel.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Discard;

        _selectedIndicies = [.. selection.SelectionIndicies()];

        selectionCenter = selection.GetCenter();
        
        int selectedCount = 0;
        foreach (uint index in selection.SelectionIndicies())
        {
            Vertex vert = activeModel.GetVertex(index);
            _startingPosition[index] = new Vector4(vert.Position.X, vert.Position.Y, vert.Position.Z, 1.0f);
            selectedCount++;
        }
        return CommandState.Idle;//Continue the command.
    }

    public void Redo()
    {
        Rotate(_totalRotation);
    }

    public void Undo()
    {
        Rotate(0);
    }
}
