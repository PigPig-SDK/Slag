using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace OpenglAvaloniaTest.Commands;

public class RotateCommand : ICommand
{
    public ICommand? Next { get; set; }
    private Vector3 _selectionCenter { get; set; }
    private Vector2? _mouseStart;
    private Dictionary<uint, Vector4> _startingPosition = [];
    private float _totalRotation;
    private float? _initialRotation = null;
    private Model? _model;
    
    private Vector3 _cameraRotationRight = Vector3.Zero;
    private Vector3 _cameraRotationUp = Vector3.Zero;
    private Vector3 _rotationUp;
    private Vector3 _rotationRight;

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel == null) return CommandState.Discard;
        
        if(args.info.HasFlag(CommandInfo.Initialization)) return Initialize();

        //Is a mouse input
        if ((args.info & CommandInfo.MouseEvent) != 0)
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
            if (args.info.HasFlag(CommandInfo.MouseDown))
                return CommandState.Finished;
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
                        return CommandState.Finished;
                    }
            }
        }
        return CommandState.Idle;
    }

    private void Rotate(float rotationAmmount)
    {
        if(_model == null) return;

        Vector3 normal = Vector3.Cross(_rotationUp, _rotationRight);

        Matrix4 rotationMatrix =  Matrix4.CreateFromAxisAngle(normal, -rotationAmmount);
        Vertex[] vertices = _model.GetVertexBackingField();

        foreach (var pair in _startingPosition)
        {
            vertices[pair.Key].Position = _selectionCenter + (pair.Value * rotationMatrix).Xyz;
        }
        _model.UpdateAllComponents(UpdateType.Locational,null);
    }

    private CommandState Initialize()
    {
        if (Camera.Instance == null) throw new InvalidOperationException($"No camera in {nameof(MoveCommand)} {nameof(Initialize)}");
        if (GLControl.Instance == null) throw new InvalidOperationException($"No such {nameof(GLControl.Instance)}");

        _model = SelectionManager.Instance.CurrentModel;
        if (_model == null) return CommandState.Discard;//Cannot execute command

        SelectionComponent? selection = _model.GetComponent<SelectionComponent>();
        if (selection is null) return CommandState.Discard;

        _selectionCenter = selection.GetCenter();
        
        int selectedCount = 0;
        foreach (uint index in selection.SelectionIndicies())
        {
            Vertex vert = _model.GetVertex(index);
            _startingPosition[index] = new Vector4(vert.Position.X - _selectionCenter.X, vert.Position.Y - _selectionCenter.Y, vert.Position.Z - _selectionCenter.Z, 1.0f);
            selectedCount++;
        }

        //Read camera rotation vector
        var directions = Camera.Instance.GetRealitiveDirections();
        _rotationRight = _cameraRotationRight = directions.realitiveRight.Normalized();
        _rotationUp = _cameraRotationUp = directions.realitiveUp.Normalized();

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
