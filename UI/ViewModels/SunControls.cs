using OpenTK.Mathematics;

namespace UI.ViewModels;

public static class SunControls
{
    public static bool IsEnabled { get; set; } = true;
    public static Vector3 SunAngle => new Vector3(50, 50, 25).Normalized();

    public const int ShadowMapWidth = 2048, ShadowMapHeight = 2048;
    public const float SunClippingDistance = 75;
}
