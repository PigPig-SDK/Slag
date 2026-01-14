using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using Models;


public class Camera
{
    //camera controls
    private float _pitch, _yaw;
    private float _zoom = 1.0f;

    private bool _isDragging = false;
    private Point _lastDragLocation;
    private Vector3 _cameraOffset;

    private const float ZoomAmmount = 0.1f;
    private const float MoveAmmount = 0.025f;

    private OpenGlControlBase _glBase;

    public Camera(OpenGlControlBase glBase)
    {
        _glBase = glBase;
    }

    public void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        _zoom -= ZoomAmmount * (float)e.Delta.Y;
    }

    public void OnPointerMove(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!_isDragging) return;
        Point dragDelta = _lastDragLocation - e.GetPosition(_glBase);
        _lastDragLocation = e.GetPosition(_glBase);

        if (e.GetCurrentPoint(_glBase).Properties.IsMiddleButtonPressed)
        {
            //Compute realitive directions
            (Vector3 realitiveRight, Vector3 realitiveUp) dirs = GetRealitiveDirections();
            _cameraOffset += (dirs.realitiveRight * (float)dragDelta.X + dirs.realitiveUp * (float)dragDelta.Y) * MoveAmmount;
        }
        else
        {
            _pitch -= (float)dragDelta.Y * 0.01f;
            _yaw += (float)dragDelta.X * 0.01f;
        }
    }

    public void OnMouseUp(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    public RaycastHit? FindRaycastHit(Vector2 screenLocation)
    {
        double width = _glBase.Bounds.Width;
        double height = _glBase.Bounds.Height;
        float aspect = (float)(width / height);
        Vector2 screenSize = new Vector2((float)width, (float)height);

        return Raycast.GetObjectHitScreenLocation(SceneHierarchy.Instance.Models, Origin, Up, LookAt, aspect, FOV, screenLocation, screenSize);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(_glBase).Properties;
        if(properties.IsRightButtonPressed)
        {
            Console.WriteLine("Detect ray in scene");
            Vector2 screenLocation = new Vector2((float)e.GetPosition(_glBase).X, (float)e.GetPosition(_glBase).Y);
            RaycastHit? hit = FindRaycastHit(screenLocation);

            if (hit != null)
            {
                Console.WriteLine($"Hit was not null! {hit}");
            }
            else
            {
                Console.WriteLine("Hit was null");
            }
            return;
        }

        _lastDragLocation = e.GetPosition(_glBase);
        _isDragging = true;
    }

    public Vector3 Origin => (new Vector3(MathF.Cos(_pitch) * MathF.Sin(_yaw), MathF.Sin(_pitch), MathF.Cos(_pitch) * MathF.Cos(_yaw)) * _zoom) + _cameraOffset;

    public Vector3 LookAt => _cameraOffset;

    public Vector3 Up => Vector3.UnitY;

    public (Vector3 realitiveRight, Vector3 realitiveUp) GetRealitiveDirections()
    {
        Vector3 lookDir = Vector3.Normalize(Origin - LookAt);
        Vector3 realitiveRight = Vector3.Cross(Up, lookDir);
        Vector3 realitiveUp = Vector3.Cross(realitiveRight, lookDir);
        return (realitiveRight, realitiveUp);
    }

    public Matrix4 CreateLookAt() => Matrix4.LookAt(Origin, LookAt, Up);

    public Matrix4 CreatePrespective(float aspect) => Matrix4.CreatePerspectiveFieldOfView(FOV, (aspect == 0)? 1.0f : aspect, 0.01f, 100000f);

    /// <summary>
    /// Gets FOV in Radians
    /// </summary>
    public float FOV => (MathF.PI/180.0f) * 90.0f;
}
