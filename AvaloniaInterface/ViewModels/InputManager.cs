using Avalonia;
using Avalonia.Input;
using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

public class InputManager
{
    public static InputManager Singleton = new InputManager();

    public void OnMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        Camera.Instance?.OnMouseUp(sender, e);
    }

    public void OnPointerMove(object? sender, PointerEventArgs e)
    {
        Camera.Instance?.OnPointerMove(sender, e);
    }

    public void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(GLControl.Instance).Properties;
        if (properties.IsRightButtonPressed)
        {
            Vector2 screenLocation = new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
            RaycastHit? hit = Camera.Instance?.FindRaycastHit(screenLocation);
            if (hit != null)
            {
                ModelSelection? ms = hit!.Model.GetComponent<ModelSelection>();

                if (ms == null) throw new InvalidOperationException("Model dosn't contain ModelSelection!");

                ms.DeselectAll(UpdateType.Ignore);
                foreach (uint index in hit!.Face.Indicies)
                {
                    ms.SelectIndex(index, UpdateType.Ignore);
                }
                ms.BroadcastMassUpdate(UpdateType.Face);
            }
            return;
        }

        Camera.Instance?.OnMouseDown(sender, e);
    }

    public void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch(e.Key)
        {
            case Key.Delete:
                {

                    break;
                }
        }
    }
}
