using OpenTK.Mathematics;

namespace Core;

public static class VectorExtensions
{
    static float Snap(float value, float step)
    {
        return MathF.Round(value / step) * step;
    }
    public static void Snap(this Vector3 vector, float step)
    {
        vector.X = Snap(vector.X, step);
        vector.Y = Snap(vector.Y, step);
        vector.Z = Snap(vector.Z, step);
    }

}
