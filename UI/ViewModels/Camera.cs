using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL.Controls;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UI.ViewModels;

public class Camera
{
    private static Camera? _instance = null!;
    public static Camera Instance
    {
        get
        {
            if (_instance == null) throw new InvalidOperationException($"No instance of {nameof(Camera)} exists!");
            return _instance;
        }
        set
        {
            if (_instance != null) throw new InvalidOperationException($"An instance of {nameof(Camera)} already exists!");
            _instance = value;
        }
    }

    //camera controls
    //Pitch and YAW are in radians
    private const float _startPitch = float.Pi / 4;
    private const float _startYaw = float.Pi / 4;
    private const float _startZoom = 5.0f;

    private float _pitch = _startPitch;
    private float _yaw = _startYaw;
    private float _zoom = _startZoom;

    private bool _isDragging;
    private Point _lastDragLocation;
    private Vector3 _cameraOffset;

    private const float ZoomAmmount = 0.1f;
    private const float MoveAmmount = 0.025f;

    private OpenGlControlBase _glBase;

    public Camera(OpenGlControlBase glBase)
    {
        Instance = this;
        _glBase = glBase;
    }

    public void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        _zoom -= ZoomAmmount * (float)e.Delta.Y;
        if (_zoom < 0.1f) _zoom = 0.1f;
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
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
            _pitch = Math.Clamp(_pitch, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);
            _yaw += (float)dragDelta.X * 0.01f;
        }
    }

    public void OnMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
    }

    public Vector2 ScreenToGlCoords(Vector2 screenPosition)
    {
        float width = (float)GLControl.Instance.Bounds.Width;
        float height = (float)GLControl.Instance.Bounds.Height;

        Vector2 pos = new Vector2(screenPosition.X / width * 2.0f - 1.0f, 1.0f - screenPosition.Y / height * 2.0f);

        return new Vector2(
        MathHelper.Clamp(pos.X, -1, 1),
        MathHelper.Clamp(pos.Y, -1, 1)
        );
    }

    public RaycastHit? FindRaycastHit(Vector2 screenLocation, IEnumerable<Model> models)
    {
        double width = _glBase.Bounds.Width;
        double height = _glBase.Bounds.Height;
        float aspect = (float)(width / height);
        Vector2 screenSize = new Vector2((float)width, (float)height);

        return Raycast.GetObjectHitScreenLocation(models, Origin, WorldUp, LookAt, aspect, FOV, screenLocation, screenSize);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        _lastDragLocation = e.GetPosition(_glBase);
        _isDragging = true;
    }

    public Vector3 Origin => new Vector3(MathF.Cos(_pitch) * MathF.Sin(_yaw), MathF.Sin(_pitch), MathF.Cos(_pitch) * MathF.Cos(_yaw)) * _zoom + _cameraOffset;

    public Vector3 LookAt => _cameraOffset;

    public static Vector3 WorldUp => Vector3.UnitY;

    public Vector3 Right => GetRealitiveDirections().realitiveRight;

    public Vector3 Up => GetRealitiveDirections().realitiveUp;

    public (Vector3 realitiveRight, Vector3 realitiveUp) GetRealitiveDirections()
    {
        Vector3 lookDir = Vector3.Normalize(Origin - LookAt);
        Vector3 realitiveRight = Vector3.Cross(WorldUp, lookDir);
        Vector3 realitiveUp = Vector3.Cross(realitiveRight, lookDir);
        return (realitiveRight.Normalized(), realitiveUp.Normalized());
    }

    public Matrix4 CreateLookAt() => Matrix4.LookAt(Origin, LookAt, WorldUp);

    public Matrix4 CreatePrespective(float aspect)
    {
        //TODO: Test orthographic projection
        //return Matrix4.CreateOrthographic(10*aspect, 10, 0, 1000);

        return Matrix4.CreatePerspectiveFieldOfView(FOV, aspect == 0 ? 1.0f : aspect, 0.01f, 100000f);
    }

    public Matrix4 ViewMatrix => CreateLookAt() * CreatePrespective(GLControl.Instance!.Aspect);

    public Vector2 WorldToScreen(Vector3 position)
    {
        Matrix4 matrix = ViewMatrix;
        Vector4 p4 = new Vector4(position);
        p4.W = 1.0f;
        p4 = p4 * matrix;//translate to clip space
        p4 /= p4.W;
        Vector2 worldToScreen = p4.Xy;

        //To resolution
        worldToScreen.X = (float)((worldToScreen.X + 1.0) / 2.0 * GLControl.Instance.Bounds.Width);
        worldToScreen.Y = (float)((worldToScreen.Y + 1.0) / 2.0 * GLControl.Instance.Bounds.Height);
        worldToScreen.Y = (float)GLControl.Instance.Bounds.Height - worldToScreen.Y; // flip Y

        return worldToScreen;
    }

    public void Reset()
    {
        _pitch = _startPitch;
        _yaw = _startYaw;
        _zoom = _startZoom;
        _cameraOffset = new Vector3(0, 0, 0);
    }

    /// <summary>
    /// Gets FOV in Radians
    /// </summary>
    public static float FOV => MathF.PI / 180.0f * 90.0f;

}