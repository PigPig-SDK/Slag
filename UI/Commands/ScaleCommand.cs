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

namespace UI.Commands;

public class ScaleCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Scale";

    public string Description =>
    "[X, Y, Z] : Specify a scale axis\n" +
    "[S, Click] : Accept changes\n" +
    "[ESC] : Decline changes";

    public bool DisplayToolText => true;

    private const float _moveDistanceScale = 0.005f;
    public Vector3 SelectionCenter = new Vector3(0, 0, 0);
    private Vector3 _activeAxis = new Vector3(1, 1, 1);
    private Vector2 _mouseScreenCenter = Vector2.Zero;
    private List<uint>? _selectedIndicies = null;

    private Dictionary<uint, (Vector3 position, Vector3 moveNormal)> _StartingPosition = [];
    private float _ScaleValue;

    //UI stuff..
    private Line? _uiLine;
    private TextBlock? _textblock;
    public bool ActiveAxisOverride = false;

    private CommandState Initialize()
    {
        if (Camera.Instance == null) throw new InvalidOperationException($"No camera in {nameof(MoveCommand)} {nameof(Initialize)}");
        if (GLControl.Instance == null) throw new InvalidOperationException($"No such {nameof(GLControl.Instance)}");

        Model? activeModel = SelectionManager.Instance.CurrentModel;
        if (activeModel == null) return CommandState.Discard;//Cannot execute command

        SelectionComponent? selection = activeModel.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Discard;

        _selectedIndicies = [.. selection.GetSelection<uint>()];

        SelectionCenter = selection.GetCenter();
        //Compute center.
        _mouseScreenCenter = Camera.Instance.WorldToScreen(selection.GetWorldCenter());
        int selectedCount = 0;
        foreach (uint index in selection.GetSelection<uint>())
        {
            Vertex vert = activeModel.GetVertex(index);
            _StartingPosition[index] = (vert.Position, (SelectionCenter - vert.Position).Normalized());
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
            ActiveAxisOverride = false;
            _activeAxis = new Vector3(1, 1, 1);
        }
    }
    private void Scale(float ammount)
    {
        Model? model = SelectionManager.Instance.CurrentModel;
        if (model == null) throw new Exception("No current model in MoveCommand.MoveSelection");

        if (_selectedIndicies == null) throw new Exception($"No selection set {nameof(_selectedIndicies)}!");

        Vertex[] vertices = model.GetVertexBackingField();

        foreach (uint index in _selectedIndicies)
        {
            vertices[index].Position = _StartingPosition[index].position + ((_StartingPosition[index].moveNormal* ammount) * _activeAxis);
        }
        model.UpdateAllComponents(UpdateType.Locational);
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
            _ScaleValue = (250 - mouseDelta.Length) * _moveDistanceScale;

            if(_textblock is not null)
            {
                Vector2 textPosition = _mouseScreenCenter + mouseDelta/2;
                _textblock.IsVisible = true;
                _textblock!.RenderTransform = new TranslateTransform(textPosition.X, textPosition.Y);
                _textblock.Text = (_ScaleValue*-1).ToString("F2");
            }    

            Scale(_ScaleValue);
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
                    Scale(0.0f);
                    CleanUpUi();
                    return CommandState.Finished;
                }

        }

        return CommandState.Idle;
    }
    private void CleanUpUi()
    {
        if (MainWindow.Instance == null) return;
        Canvas canvas = MainWindow.Instance.OverlayCanvas;

        if(_uiLine is not null)
            canvas.Children.Remove(_uiLine);
        if (_textblock is not null)
            canvas.Children.Remove(_textblock);
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
