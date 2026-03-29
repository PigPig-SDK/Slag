using OpenTK.Mathematics;

namespace Core;

public static class LineIntersection
{
    /// <summary>
    /// Finds the intersection point of two 2D line segments, if it exists.
    /// </summary>
    /// <param name="p1">Start point of segment 1</param>
    /// <param name="p2">End point of segment 1</param>
    /// <param name="p3">Start point of segment 2</param>
    /// <param name="p4">End point of segment 2</param>
    /// <param name="intersectionPoint">Output intersection point (only valid if method returns true)</param>
    /// <returns>True if the segments intersect, false otherwise (including parallel or collinear)</returns>
    public static bool FindSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectionPoint)
    {
        intersectionPoint = Vector2.Zero;

        Vector2 s1 = p2 - p1;
        Vector2 s2 = p4 - p3;

        float denominator = s1.X * s2.Y - s2.X * s1.Y;

        if (Math.Abs(denominator) < 0.00001f)
        {
            return false;
        }

        Vector2 p1p3 = p3 - p1;
        float numerator1 = p1p3.X * s2.Y - s2.X * p1p3.Y;
        float numerator2 = p1p3.X * s1.Y - s1.X * p1p3.Y;

        float u = numerator1 / denominator;
        float v = numerator2 / denominator;

        if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
        {
            intersectionPoint = p1 + s1 * u;
            return true;
        }

        return false;
    }
}
