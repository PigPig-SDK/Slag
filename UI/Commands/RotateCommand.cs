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

public class RotateCommand : ICommand
{
    public ICommand? Next { get; set; }
    private Vector3 _selectionCenter { get; set; }

    public string Name => "Rotate";

    public string Description => 
        "[X, Y, Z] : Specify a rotation axis\n" +
        "[HOLD CTRL] : Lock to a unit of 11.25°\n" +
        "[R, Click] : Accept changes\n" +
        "[ESC] : Decline changes";

    public bool DisplayToolText => true;

    private Vector2? _mouseStart;
    private Dictionary<uint, Vector4> _startingPosition = [];
    private float _totalRotation;
    private float? _initialRotation = null;
    private Model? _model;
    
    private Vector3 _cameraRotationRight = Vector3.Zero;
    private Vector3 _cameraRotationUp = Vector3.Zero;
    private Vector3 _rotationUp;
    private Vector3 _rotationRight;

    private float _uiRadius = 200;

    private Line? _initialLine;
    private Line? _dynamicLine;
    private Ellipse? _outlineElipse;
    private TextBlock? _textblock;

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;
        
        if(args.info.HasFlag(CommandInfo.Initialization)) return Initialize();

        //Is a mouse input
        if ((args.info & CommandInfo.MouseEvent) != 0)
        {
            var mouseInfo = args.mouseEvent!.GetPosition(GLControl.Instance);
            Vector2 mousePos = new((float)mouseInfo.X, (float)mouseInfo.Y);
            if (_mouseStart is null)
            {
                _mouseStart = mousePos;
                _initialLine!.StartPoint = new(_mouseStart.Value.X, _mouseStart.Value.Y);
                _dynamicLine!.StartPoint = _initialLine!.StartPoint;
            }
            Vector2 distanceVector = mousePos - _mouseStart.Value;
            if(distanceVector.LengthSquared == 0) return CommandState.Idle;//Cannot compute right now.
            float angle = MathF.Atan2(distanceVector.Y, distanceVector.X);

            //Handle UI
            if (_initialRotation is null)
            {
                _initialRotation = angle;
                Vector2 anglePos = _mouseStart.Value + (distanceVector.Normalized() * _uiRadius/2);

                _textblock!.RenderTransform = new TranslateTransform(_mouseStart.Value.X, (_mouseStart.Value.Y - _uiRadius / 2) - 30);
                _initialLine!.EndPoint = new(anglePos.X, anglePos.Y);
                _outlineElipse!.RenderTransform = new TranslateTransform(_mouseStart.Value.X - _uiRadius/2, _mouseStart.Value.Y - _uiRadius/2);
                
                //Make UI appear...
                _outlineElipse.IsVisible = true;
                _initialLine.IsVisible = true;
                _textblock.IsVisible = true;
            }

            //Dynamic UI compute
            Vector2 desiredPos = _mouseStart.Value + (distanceVector.Normalized() * Math.Min(_uiRadius / 2, distanceVector.Length));
            _dynamicLine!.EndPoint = new(desiredPos.X, desiredPos.Y);
            _dynamicLine!.IsVisible = true;

            _totalRotation = angle - _initialRotation.Value;

            if (args.mouseEvent.KeyModifiers == KeyModifiers.Control)
            {
                float clampFactor = _totalRotation % (MathF.PI / 16);//Snap to 11.25 degree increments when ctrl is held.
                _totalRotation -= clampFactor;
            }

            _textblock!.Text = (_totalRotation * (180.0 / MathF.PI)).ToString("F1", CultureInfo.InvariantCulture);

            Rotate(_totalRotation);
            if (args.info.HasFlag(CommandInfo.MouseDown))
            {
                CleanUpUi();
                return CommandState.Finished;
            }
        }

        if(args.info.HasFlag(CommandInfo.KeyDown))
        {
            switch(args.keyEvent!.Key)
            {
                case Key.X:
                    {
                        _rotationRight = new Vector3(0, 1, 0);
                        _rotationUp = new Vector3(0, 0, 1);
                        break;
                    }
                case Key.Y:
                    {
                        _rotationRight = new Vector3(1, 0, 0);
                        _rotationUp = new Vector3(0, 0, 1);
                        break;
                    }
                case Key.Z:
                    {
                        _rotationRight = new Vector3(1, 0, 0);
                        _rotationUp = new Vector3(0, 1, 0);
                        break;
                    }
                case Key.R:
                    {
                        CleanUpUi();
                        return CommandState.Finished;
                    }
                case Key.Escape:
                    {
                        Rotate(0.0f);
                        CleanUpUi();
                        return CommandState.Finished;
                    }
            }
        }
        return CommandState.Idle;
    }
    private void CleanUpUi()
    {
        Canvas canvas = MainWindow.Instance.OverlayCanvas;

        if (_initialLine is not null)
            canvas.Children.Remove(_initialLine);
        if(_outlineElipse is not null)
            canvas.Children.Remove(_outlineElipse);
        if(_dynamicLine is not null)
            canvas.Children.Remove(_dynamicLine);
        if(_textblock is not null)
            canvas.Children.Remove(_textblock);
    }
    private void Rotate(float rotationAmmount)
    {
        if(_model == null) return;

        Vector3 normal = Vector3.Cross(_rotationRight, _rotationUp);

        Matrix4 rotationMatrix =  Matrix4.CreateFromAxisAngle(normal, rotationAmmount);
        Vertex[] vertices = _model.GetVertexBackingField();

        foreach (var pair in _startingPosition)
        {
            vertices[pair.Key].Position = _selectionCenter + (pair.Value * rotationMatrix).Xyz;
        }
        _model.UpdateAllComponents(UpdateType.Locational);
    }

    private CommandState Initialize()
    {
        _model = SelectionManager.Instance.CurrentModel;
        if (_model == null) return CommandState.Discard;//Cannot execute command

        SelectionComponent? selection = _model.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Discard;

        _selectionCenter = selection.GetCenter();
        
        int selectedCount = 0;
        foreach (uint index in selection.GetSelection<uint>())
        {
            Vertex vert = _model.GetVertex(index);
            _startingPosition[index] = new Vector4(vert.Position.X - _selectionCenter.X, vert.Position.Y - _selectionCenter.Y, vert.Position.Z - _selectionCenter.Z, 1.0f);
            selectedCount++;
        }

        //Read camera rotation vector
        var directions = Camera.Instance.GetRealitiveDirections();
        _rotationRight = _cameraRotationRight = directions.realitiveRight.Normalized();
        _rotationUp = _cameraRotationUp = directions.realitiveUp.Normalized();

        //UI
        if (MainWindow.Instance != null)
        {
            _initialLine = new()
            {
                StartPoint = new(0, 0),
                EndPoint = new(0, 0),
                Stroke = SelectionManager.SelectionColor,
                StrokeThickness = 2,
                StrokeDashArray = [4, 4],
                IsVisible = false
            };
            _dynamicLine = new()
            {
                StartPoint = new(0, 0),
                EndPoint = new(0, 0),
                Stroke = SelectionManager.SelectionColor,
                StrokeThickness = 3,
                IsVisible = false
            };
            _outlineElipse = new Ellipse
            {
                Width = _uiRadius,
                Height = _uiRadius,
                Stroke = SelectionManager.SelectionColor,
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                StrokeDashArray = new AvaloniaList<double> { 4, 4 },
                IsVisible = false,
            };

            _textblock = new()
            {
                Text = "",
                Foreground = SelectionManager.SelectionColor,
                FontSize = 14,
                IsVisible=false,
            };

            Canvas canvas = MainWindow.Instance.OverlayCanvas;
            canvas.Children.Add(_initialLine);
            canvas.Children.Add(_outlineElipse);
            canvas.Children.Add(_dynamicLine);
            canvas.Children.Add(_textblock);
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
