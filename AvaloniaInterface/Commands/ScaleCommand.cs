using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenglAvaloniaTest.Commands;

public class ScaleCommand : ICommand
{
    public ICommand? Next { get; set; }

    private const float _MoveDistanceScale = 0.005f;
    public Vector3 SelectionCenter = new Vector3(0, 0, 0);
    private Vector3 _ActiveAxis = new Vector3(1, 1, 1);
    private Vector2 _MouseScreenCenter = Vector2.Zero;
    private List<uint>? _SelectedIndicies = null;

    private Dictionary<uint, (Vector3 position, Vector3 moveNormal)> _StartingPosition = [];
    private float _ScaleValue;

    private CommandState Initialize()
    {
        if (Camera.Instance == null) throw new InvalidOperationException($"No camera in {nameof(MoveCommand)} {nameof(Initialize)}");
        if (GLControl.Instance == null) throw new InvalidOperationException($"No such {nameof(GLControl.Instance)}");

        Model? activeModel = SelectionManager.Instance.CurrentModel;
        if (activeModel == null) return CommandState.Finished;//Cannot execute command

        SelectionComponent? selection = activeModel.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Finished;

        _SelectedIndicies = [.. selection.SelectionIndicies()];

        SelectionCenter = selection.GetCenter();
        //Compute center.
        _MouseScreenCenter = Camera.Instance.WorldToScreen(selection.GetWorldCenter());
        int selectedCount = 0;
        foreach (uint index in selection.SelectionIndicies())
        {
            Vertex vert = activeModel.GetVertex(index);
            _StartingPosition[index] = (vert.Position, (SelectionCenter - vert.Position).Normalized());
            selectedCount++;
        }
        return CommandState.Idle;//Continue the command.
    }

    private void Scale(float ammount)
    {
        Model? model = SelectionManager.Instance.CurrentModel;
        if (model == null) throw new Exception("No current model in MoveCommand.MoveSelection");

        if (_SelectedIndicies == null) throw new Exception($"No selection set {nameof(_SelectedIndicies)}!");

        Vertex[] vertices = model.GetVertexBackingField();

        foreach (uint index in _SelectedIndicies)
        {
            vertices[index].Position = _StartingPosition[index].position + ((_StartingPosition[index].moveNormal* ammount) * _ActiveAxis);
        }
        model.UpdateAllComponents(UpdateType.Locational, null);
    }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        //No model, no command.
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Finished;
        //Initialization
        if (args.info.HasFlag(CommandInfo.Initialization)) return Initialize();
        //Block keyup inputs
        if (args.info.HasFlag(CommandInfo.KeyUp)) return CommandState.Idle;

        //Is a mouse input
        if ((args.info & CommandInfo.MouseEvent) != 0)
        {
            var mouseInfo = args.mouseEvent!.GetPosition(GLControl.Instance);
            Vector2 mouseDelta = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y) - _MouseScreenCenter;
            _ScaleValue = (250 - mouseDelta.Length) * _MoveDistanceScale;
            Scale(_ScaleValue);
            if (args.info.HasFlag(CommandInfo.MouseDown))//Accept.
                return CommandState.Finished;
        }

        //Keyboard input
        switch (args.keyEvent?.Key)
        {
            case Key.S://Accept.
                return CommandState.Finished;
            case Key.X:
                _ActiveAxis = new Vector3(1, 0, 0);
                return CommandState.Idle;
            case Key.Y:
                _ActiveAxis = new Vector3(0, 1, 0);
                return CommandState.Idle;
            case Key.Z:
                _ActiveAxis = new Vector3(0, 0, 1);
                return CommandState.Idle;
            case Key.Escape:
                {
                    Scale(0.0f);
                    return CommandState.Finished;
                }

        }

        return CommandState.Idle;
    }
    public void Undo()
    {
        Scale(0);
    }
    public void Redo()
    {
        Scale(_ScaleValue);
    }
}
