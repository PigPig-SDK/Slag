using Avalonia.Input;
using OpenTK.Mathematics;


namespace UI.ViewModels;

public static class PointerEventArgsExtentions
{   
    public static Vector2 GetScreenPos(this PointerPressedEventArgs e)
    {
        return new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
    }
    public static Vector2 GetScreenPos(this PointerEventArgs e)
    {
        return new Vector2((float)e.GetPosition(GLControl.Instance).X, (float)e.GetPosition(GLControl.Instance).Y);
    }
}