using OpenTK.Mathematics;

namespace Core;

public static class VectorExtensions
{
    static float Snap(float value, float step)
    {
        return MathF.Round(value / step) * step;
    }

    /// <summary>
    /// Snaps vector to a specific unit value
    /// </summary>
    /// <param name="step">How often the value is 'snapped'</param>
    /// <remarks>If Step is zero, than nothing happens.</remarks>
    public static void Snap(this ref Vector3 vector, float step)
    {
        if (step == 0) return;//Simply return the identiy

        vector.X = Snap(vector.X, step);
        vector.Y = Snap(vector.Y, step);
        vector.Z = Snap(vector.Z, step);
    }

}
