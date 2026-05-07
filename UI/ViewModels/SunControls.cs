using OpenTK.Mathematics;

namespace UI.ViewModels;

public static class SunControls
{
    public static bool IsEnabled { get; set; } = true;
    public static Vector3 SunAngle => new Vector3(50, 50, 25).Normalized();

    public const int _shadowWidth = 2048, _shadowHeight = 2048;
    public const float _sunDistance = 75;
}
