using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using UI.ViewModels;
using UI.Views;

namespace UI.Commands;

public class MoveCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Move";
    public override string ToString() => Name;

    public string Description =>
        "[X, Y, Z, N] : Specify a move axis/normal\n" +
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

    private bool _useNormal;
    private Vector3 _normal = new(0, 0, 0);

    private readonly Dictionary<uint, Vector3> _startingPosition = [];
    private List<uint> _selectedIndices = [];
    private Vector2 _moveDistance;
    private Vector3 _selectionCenter = Vector3.Zero;
    private Model _model = null!;

    private List<Model> _models = [];
    private readonly Dictionary<Model, Vector3> _modelsStartingPosition = [];
    private bool _isModelMove;
    private float _snapValue;

    //UI stuff..
    private Line? _uiLine;
    private TextBlock? _textblock;
    private CommandState Initialize()
    {
        _isModelMove = SelectionManager.Instance.CurrentSelectionMode == ViewModels.SelectionMode.Mesh;
        _cameraMoveDirections = Camera.Instance.GetRealitiveDirections();
        _snapValue = SelectionManager.Instance.SnapValue;
        if (_isModelMove == false)
        {
            //No model, no command.
            if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;

            _model = SelectionManager.Instance.CurrentModel!;
            SelectionComponent? selection = _model.GetComponent<SelectionComponent>();
            if (selection is null) return CommandState.Discard;

            _selectionCenter = selection.GetCenter();
            _selectedIndices = [.. selection.GetSelection<uint>()];

            ComputeSelectionNormal(selection);

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

        //UI
        if (MainWindow.Instance != null)
        {
            _uiLine = new()
            {
                StartPoint = new(0, 0),
                EndPoint = new(0, 0),
                Stroke = SelectionManager.SelectionColor,
                StrokeThickness = 2,
                StrokeDashArray = new AvaloniaList<double> { 4, 4 }
            };

            _textblock = new()
            {
                Text = "",
                Foreground = SelectionManager.SelectionColor,
                FontSize = 14,
                IsVisible = false,
            };

            MainWindow.Instance.OverlayCanvas.Children.Add(_uiLine);
            MainWindow.Instance.OverlayCanvas.Children.Add(_textblock);
        }

        return CommandState.Idle;//Continue the command.
    }

    private void ComputeSelectionNormal(SelectionComponent selection)
    {
        int totalSum = 0;
        foreach(Face face in selection.SelectedFaces)
        {
            _normal += face.GetNormal();
            totalSum++;
        }

        foreach(Edge edge in selection.SelectedEdges)
        {
            _normal += edge.GetAssumedNormal();
            totalSum++;
        }

        if (totalSum == 0) return;//Erm. bazinga?

        _normal/= totalSum;
        _normal.Normalize();
    }

    void CleanUp()
    {
        foreach(Model model in EditVisualizers.Instance.AllVisualizers)
        {
            model.Hidden = true;
            model.Position = Vector3.Zero;
        }
        CleanUpUi();
    }

    private void MoveSelection(Vector2 mouseDelta)
    {
        Vector3 moveDirection = (_cameraMoveDirections.realitiveRight * mouseDelta.X) + (_cameraMoveDirections.realitiveUp * mouseDelta.Y);
        if (_useNormal)
            moveDirection = _normal * mouseDelta.X;

        moveDirection *= _moveDistanceScale;

        if (_isModelMove)
        {
            moveDirection.Snap(_snapValue);

            foreach (Model model in _models)
            {
                model.Position = _modelsStartingPosition[model] + (moveDirection * _activeAxis);
            }
        }
        else//Move interior of mesh
        {
            moveDirection *= _activeAxis;
            Vector4 moveDir4 = new(moveDirection, 1.0f);
            var modelMatrix = _model.GetRotationMatrix();
            moveDir4 = modelMatrix * moveDir4;
            moveDir4 /= moveDir4.W;
            moveDirection = moveDir4.Xyz;

            Vertex[] vertices = _model.GetVertexBackingField();

            foreach (uint index in _selectedIndices)
            {
                Vector3 newPos = new(_startingPosition[index] + moveDirection);
                newPos.Snap(_snapValue);
                vertices[index].Position = newPos;
            }

            Vector3 visualizerPosition = _selectionCenter + moveDirection;

            EditVisualizers.Instance.AxisVisualizerY.Position = visualizerPosition * new Vector3(0, 1, 0);
            EditVisualizers.Instance.AxisVisualizerX.Position = visualizerPosition * new Vector3(0, 0, 1);
            EditVisualizers.Instance.AxisVisualizerZ.Position = visualizerPosition * new Vector3(1, 0, 0);

            _model.UpdateAllComponents(UpdateType.Locational);
        }
    }

    public CommandState Execute(CommandArguments args)
    {
        //Initialization
        if (args.CommandInfo.HasFlag(CommandInfo.Initialization)) return Initialize();
        //Block keyup inputs
        if (args.CommandInfo.HasFlag(CommandInfo.KeyUp)) return CommandState.Idle;

        //Is a mouse input
        if ((args.CommandInfo & CommandInfo.MouseEvent) != 0)
        {
            var mouseInfo = args.MouseEvent!.GetPosition(GLControl.Instance);
            Vector2 mousePos = new((float)mouseInfo.X, (float)mouseInfo.Y);
            _mouseStartPos ??= mousePos;
            _moveDistance = mousePos - _mouseStartPos.Value;

            _uiLine!.StartPoint = new Avalonia.Point(_mouseStartPos.Value.X, _mouseStartPos.Value.Y);
            _uiLine!.EndPoint = new Avalonia.Point(mousePos.X, mousePos.Y);
            if (_textblock is not null)
            {
                Vector2 textPosition = _mouseStartPos.Value + _moveDistance / 2;
                _textblock.IsVisible = true;
                _textblock!.RenderTransform = new TranslateTransform(textPosition.X, textPosition.Y);
                _textblock.Text = (_moveDistance.LengthFast * _moveDistanceScale).ToString("F2", CultureInfo.InvariantCulture);
            }

            MoveSelection(_moveDistance);

            if (args.CommandInfo.HasFlag(CommandInfo.MouseDown))
            {
                CleanUp();
                return CommandState.Finished;
            }
        }

        //Keyboard input
        switch(args.KeyPressEvent?.Key)
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
            case Key.N:
                {
                    //Cannot use normal mode without a valid normal.
                    if(_normal.LengthSquared == 0) return CommandState.Idle;
                    _useNormal = !_useNormal;
                    return CommandState.Idle;
                }
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
        float snapValueCached = _snapValue;
        _snapValue = 0;
        MoveSelection(Vector2.Zero);
        _snapValue = snapValueCached;
    }

    public void Redo()
    {
        MoveSelection(_moveDistance);
    }
    private void CleanUpUi()
    {
        Canvas canvas = MainWindow.Instance.OverlayCanvas;

        if (_uiLine is not null)
            canvas.Children.Remove(_uiLine);
        if (_textblock is not null)
            canvas.Children.Remove(_textblock);
    }
}
