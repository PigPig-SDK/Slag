using Avalonia.Input;
using UI.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Core;
using System.Reflection.Metadata.Ecma335;

namespace UI.Commands;

public class MoveCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Move";
    public override string ToString() => Name;

    public string Description =>
        "[X, Y, Z] : Specify a move axis\n" +
        "[SHIFT] : Snap to cursor" +
        "[G, Click] : Accept changes\n" +
        "[ESC] : Decline changes";

    public bool DisplayToolText => true;

    public string IconSource => "avares://Slag/Assets/icons/move.png";

    public bool AllowInMeshMode => true;

    private const float _moveDistanceScale = 0.01f;
    private (Vector3 realitiveRight, Vector3 realitiveUp) _cameraMoveDirections;
    private Vector2? _mouseStartPos;
    private bool _activeAxisOverride;
    private Vector3 _activeAxis = new(1, 1, 1);

    private Dictionary<uint, Vector3> _startingPosition = [];
    private List<uint> _selectedIndices = [];
    private Vector2 _moveDistance;
    private Vector3 _selectionCenter = Vector3.Zero;
    private Model _model = null!;

    private List<Model> _models = [];
    private Dictionary<Model, Vector3> _modelsStartingPosition = [];
    private bool _isModelMove;
    private CommandState Initialize()
    {
        _isModelMove = SelectionManager.Instance.CurrentSelectionMode == SelectionMode.Mesh;
        _cameraMoveDirections = Camera.Instance.GetRealitiveDirections();

        if (_isModelMove == false)
        {
            //No model, no command.
            if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;

            _model = SelectionManager.Instance.CurrentModel!;
            SelectionComponent? selection = _model.GetComponent<SelectionComponent>();
            if (selection is null) return CommandState.Discard;

            _selectionCenter = selection.GetCenter();
            _selectedIndices = [.. selection.GetSelection<uint>()];

            foreach (uint index in _selectedIndices)
            {
                Vertex vert = _model.GetVertex(index);
                _startingPosition[index] = vert.Position;
            }
        }
        else
        {
            _models = [.. SelectionManager.Instance.CurrentBroadModels];
            foreach (Model model in _models)
            {
                _modelsStartingPosition.Add(model, model.Position);
            }
        }
        return CommandState.Idle;//Continue the command.
    }

    static void CleanUp()
    {
        foreach(Model model in EditVisualizers.Instance.AllVisualizers)
        {
            model.Hidden = true;
            model.Position = Vector3.Zero;
        }
    }

    private void MoveSelection(Vector2 mouseDelta)
    {
        Vector3 moveDirection = (_cameraMoveDirections.realitiveRight * mouseDelta.X) + (_cameraMoveDirections.realitiveUp * mouseDelta.Y);
        moveDirection *= _moveDistanceScale;

        if (_isModelMove)
        {
            foreach (Model model in _models)
            {
                model.Position = _modelsStartingPosition[model] + (moveDirection * _activeAxis);
            }
        }
        else//Move interior of mesh
        {
            Vertex[] vertices = _model.GetVertexBackingField();

            foreach (uint index in _selectedIndices)
            {
                vertices[index].Position = (_startingPosition[index] + (moveDirection * _activeAxis));
            }

            Vector3 visualizerPosition = _selectionCenter + (moveDirection * _activeAxis);

            EditVisualizers.Instance.AxisVisualizerY.Position = visualizerPosition * new Vector3(0, 1, 0);
            EditVisualizers.Instance.AxisVisualizerX.Position = visualizerPosition * new Vector3(0, 0, 1);
            EditVisualizers.Instance.AxisVisualizerZ.Position = visualizerPosition * new Vector3(1, 0, 0);

            _model.UpdateAllComponents(UpdateType.Locational);
        }
    }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
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

            if (args.info.HasFlag(CommandInfo.MouseDown))
            {
                CleanUp();
                return CommandState.Finished;
            }
        }

        //Keyboard input
        switch(args.keyEvent?.Key)
        {
            case Key.G:
                {
                    CleanUp();
                    return CommandState.Finished;
                }
            case Key.X:
                if (_activeAxisOverride == false)
                {
                    _activeAxisOverride = true;
                    _activeAxis = new Vector3(1, 0, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X == 0 ? 1 : 0, _activeAxis.Y, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Y:
                if (_activeAxisOverride == false)
                {
                    _activeAxisOverride = true;
                    _activeAxis = new Vector3(0, 1, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X, _activeAxis.Y == 0 ? 1 : 0, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Z:
                if (_activeAxisOverride == false)
                {
                    _activeAxisOverride = true;
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
                    CleanUp();
                    return CommandState.Finished;
                }
        }
        
        return CommandState.Idle;
    }
    void ActiveAxisZeroCheck()
    {
        EditVisualizers.Instance.AxisVisualizerZ.Hidden = _activeAxis.Z == 0;
        EditVisualizers.Instance.AxisVisualizerY.Hidden = _activeAxis.Y == 0;
        EditVisualizers.Instance.AxisVisualizerX.Hidden = _activeAxis.X == 0;

        if (_activeAxis == Vector3.Zero)
        {
            _activeAxisOverride = false;
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
