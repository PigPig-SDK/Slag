using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Core;
using UI.ViewModels;
using UI.Views;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UI.Commands;

public class ScaleCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Scale";

    public override string ToString() => Name;

    public string IconSource => "avares://Slag/Assets/icons/scale.png";

    public string Description =>
    "[X, Y, Z] : Specify a scale axis\n" +
    "[S, Click] : Accept changes\n" +
    "[ESC] : Decline changes";

    public bool DisplayToolText => true;

    public bool AllowInMeshMode => true;

    private const float _moveDistanceScale = 0.005f;
    private Vector3 _selectionCenter = new(0, 0, 0);
    private Vector3 _activeAxis = new(1, 1, 1);
    private Vector2 _mouseScreenCenter = Vector2.Zero;
    private List<uint>? _selectedIndices;

    private Dictionary<uint, (Vector3 position, Vector3 moveNormal)> _startingPosition = [];
    private float _scaleValue;

    //UI stuff..
    private Line? _uiLine;
    private TextBlock? _textblock;
    private bool activeAxisOverride;

    private Model _activeModel = null!;

    private CommandState Initialize()
    {
        _activeModel = SelectionManager.Instance.CurrentModel ?? throw new InvalidOperationException("_active model cannot be null!");

        SelectionComponent? selection = _activeModel.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Discard;

        _selectedIndices = [.. selection.GetSelection<uint>()];

        _selectionCenter = selection.GetCenter();
        //Compute center.
        _mouseScreenCenter = Camera.Instance.WorldToScreen(selection.GetWorldCenter());
        int selectedCount = 0;
        foreach (uint index in selection.GetSelection<uint>())
        {
            Vertex vert = _activeModel.GetVertex(index);
            _startingPosition[index] = (vert.Position, (_selectionCenter - vert.Position).Normalized());
            selectedCount++;
        }
        //UI
        if (MainWindow.Instance != null)
        {
            _uiLine = new()
            {
                StartPoint = new(_mouseScreenCenter.X, _mouseScreenCenter.Y),
                EndPoint = new(_mouseScreenCenter.X, _mouseScreenCenter.Y),
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

    void ActiveAxisZeroCheck()
    {
        if (_activeAxis == Vector3.Zero)
        {
            activeAxisOverride = false;
            _activeAxis = new Vector3(1, 1, 1);
        }
    }
    private void Scale(float amount)
    {
        if (_selectedIndices == null) throw new InvalidOperationException($"No selection set {nameof(_selectedIndices)}!");

        Vertex[] vertices = _activeModel.GetVertexBackingField();

        foreach (uint index in _selectedIndices)
        {
            Vector3 offset = _startingPosition[index].position - _selectionCenter;
            vertices[index].Position = _selectionCenter + (offset * _activeAxis) * amount;
        }

        _activeModel.UpdateAllComponents(UpdateType.Locational);
    }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        //No model, no command.
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;
        //Initialization
        if (args.info.HasFlag(CommandInfo.Initialization)) return Initialize();
        //Block keyup inputs
        if (args.info.HasFlag(CommandInfo.KeyUp)) return CommandState.Idle;

        //Is a mouse input
        if ((args.info & CommandInfo.MouseEvent) != 0)
        {
            var mouseInfo = args.mouseEvent!.GetPosition(GLControl.Instance);

            if(_uiLine is not null)
                _uiLine.EndPoint = new(mouseInfo.X, mouseInfo.Y);

            Vector2 mouseDelta = new Vector2((float)mouseInfo.X, (float)mouseInfo.Y) - _mouseScreenCenter;
            _scaleValue = mouseDelta.Length * _moveDistanceScale;

            if(_textblock is not null)
            {
                Vector2 textPosition = _mouseScreenCenter + mouseDelta/2;
                _textblock.IsVisible = true;
                _textblock!.RenderTransform = new TranslateTransform(textPosition.X, textPosition.Y);
                _textblock.Text = (_scaleValue).ToString("F2", CultureInfo.InvariantCulture);
            }    

            Scale(_scaleValue);
            if (args.info.HasFlag(CommandInfo.MouseDown))//Accept.
            {
                CleanUpUi();
                return CommandState.Finished;
            }
        }

        //Keyboard input
        switch (args.keyEvent?.Key)
        {
            case Key.S://Accept.
                CleanUpUi();
                return CommandState.Finished;
            case Key.X:
                if (activeAxisOverride == false)
                {
                    activeAxisOverride = true;
                    _activeAxis = new Vector3(1, 0, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X == 0 ? 1 : 0, _activeAxis.Y, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Y:
                if (activeAxisOverride == false)
                {
                    activeAxisOverride = true;
                    _activeAxis = new Vector3(0, 1, 0);
                }
                else
                {
                    _activeAxis = new Vector3(_activeAxis.X, _activeAxis.Y == 0 ? 1 : 0, _activeAxis.Z);
                }
                ActiveAxisZeroCheck();
                return CommandState.Idle;
            case Key.Z:
                if (activeAxisOverride == false)
                {
                    activeAxisOverride = true;
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
                    Scale(1.0f);
                    CleanUpUi();
                    return CommandState.Finished;
                }

        }

        return CommandState.Idle;
    }
    private void CleanUpUi()
    {
        Canvas canvas = MainWindow.Instance.OverlayCanvas;

        if(_uiLine is not null)
            canvas.Children.Remove(_uiLine);
        if (_textblock is not null)
            canvas.Children.Remove(_textblock);
    }

    public void Undo()
    {
        Scale(1.0f);
    }
    public void Redo()
    {
        Scale(_scaleValue);
    }
}
