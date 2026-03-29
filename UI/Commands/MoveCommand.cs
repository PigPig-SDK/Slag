using Avalonia.Input;
using UI.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Core;

namespace UI.Commands;

public class MoveCommand : ICommand
{
    public ICommand? Next { get; set; }

    private const float _moveDistanceScale = 0.01f;
    public (Vector3 realitiveRight, Vector3 realitiveUp) CameraMoveDirections;
    private Vector2? _mouseStartPos = null;
    private bool ActiveAxisOverride = false;
    private Vector3 _activeAxis = new Vector3(1, 1, 1);

    private Dictionary<uint, Vector3> _startingPosition = [];
    private List<uint>? _selectedIndicies = null;
    private Vector2 _moveDistance;

    private CommandState Initialize()
    {
        Model? activeModel = SelectionManager.Instance.CurrentModel;
        if (activeModel == null) return CommandState.Discard;//Cannot execute command

        SelectionComponent? selection = activeModel.GetComponent<SelectionComponent>();
        if(selection is null) return CommandState.Discard;

        if(Camera.Instance == null) throw new InvalidOperationException($"No camera in {nameof(MoveCommand)} {nameof(Initialize)}");

        CameraMoveDirections = Camera.Instance.GetRealitiveDirections();
        _selectedIndicies = [..selection.GetSelection<uint>()];

        foreach (uint index in _selectedIndicies)
        {
            Vertex vert = activeModel.GetVertex(index);
            _startingPosition[index] = vert.Position;
        }

        return CommandState.Idle;//Continue the command.
    }

    private void MoveSelection(Vector2 mouseDelta)
    {
        Model? model = SelectionManager.Instance.CurrentModel;
        if (model == null) throw new Exception("No current model in MoveCommand.MoveSelection");

        if(_selectedIndicies == null) throw new Exception($"No selection exists {nameof(_selectedIndicies)}!");

        Vertex[] vertices = model.GetVertexBackingField();

        Vector3 moveDirection = (CameraMoveDirections.realitiveRight * mouseDelta.X) + (CameraMoveDirections.realitiveUp * mouseDelta.Y);
        moveDirection *= _moveDistanceScale;

        foreach (uint index in _selectedIndicies)
        {

            vertices[index].Position = (_startingPosition[index] + (moveDirection * _activeAxis));
            
        }
        model.UpdateAllComponents(UpdateType.Locational);
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
            Vector2 mousePos = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y);
            _mouseStartPos ??= mousePos;
            _moveDistance = mousePos - _mouseStartPos.Value;

            MoveSelection(_moveDistance);

            if(args.info.HasFlag(CommandInfo.MouseDown))
                return CommandState.Finished;
        }

        //Keyboard input
        switch(args.keyEvent?.Key)
        {
            case Key.G:
                return CommandState.Finished;
            case Key.X:
                if (ActiveAxisOverride == false)
                {
                    ActiveAxisOverride = true;
                    _activeAxis = new Vector3(1, 0, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X == 0 ? 1 : 0, _activeAxis.Y, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Y:
                if (ActiveAxisOverride == false)
                {
                    ActiveAxisOverride = true;
                    _activeAxis = new Vector3(0, 1, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X, _activeAxis.Y == 0 ? 1 : 0, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Z:
                if (ActiveAxisOverride == false)
                {
                    ActiveAxisOverride = true;
                    _activeAxis = new Vector3(0, 0, 1);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X, _activeAxis.Y, _activeAxis.Z == 0 ? 1 : 0);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Escape:
                {
                    MoveSelection(Vector2.Zero);
                    return CommandState.Finished;
                }
        }
        
        return CommandState.Idle;
    }
    void ActiveAxisZeroCheck()
    {
        if (_activeAxis == Vector3.Zero)
        {
            ActiveAxisOverride = false;
            _activeAxis = new Vector3(1, 1, 1);
        }
    }
    public void Undo()
    {
        MoveSelection(Vector2.Zero);
    }

    public void Redo()
    {
        MoveSelection(_moveDistance);
    }
}
