using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class Raycast
{
    public static bool HasHitBoundingBox(Model model, Vector3 origin, Vector3 direction)
    {
        return true;
    }

    public static RaycastHit? CheckForHit(Model m, (uint e1, uint e2, uint e3) incidies, Vector3 origin, Vector3 direction)
    {
        Vector3 p1 = m.Verticies[(int)incidies.e1].Position;
        Vector3 p2 = m.Verticies[(int)incidies.e2].Position;
        Vector3 p3 = m.Verticies[(int)incidies.e3].Position;

        Vector3 e1 = p2 - p1;
        Vector3 e2 = p3 - p1;

        Vector3 rayCrossE2 = Vector3.Cross(direction, e2);
        float det = Vector3.Dot(e1, rayCrossE2);

        if (det > -float.Epsilon && det < float.Epsilon) return null;

        float invDet = 1.0f / det;
        Vector3 s = origin - p1;
        float u = invDet * Vector3.Dot(s, rayCrossE2);

        if ((u < 0 && Math.Abs(u) > float.Epsilon) || (u > 1 && Math.Abs(u - 1) > float.Epsilon)) return null;

        Vector3 sCrossE1 = Vector3.Cross(s, e1);
        float v = invDet * Vector3.Dot(direction, sCrossE1);

        if ((v < 0 && Math.Abs(v) > float.Epsilon) || (u + v > 1 && Math.Abs(u + v - 1) > float.Epsilon)) return null;

        float t = invDet * Vector3.Dot(e2, sCrossE1);

        if(t > float.Epsilon)
        {
            RaycastHit hit = new()
            {
                HitPoint = (origin + direction * t),
                BarycentricPoint = new Vector3(u, v, 1 - (u + v))
            };
            return hit;
        }
        else
            return null;
    }

    public static RaycastHit? GetObjectHitScreenLocation(List<Model> models, Vector3 origin, Vector3 cameraUp, Vector3 lookLocation, float aspect, float fov, Vector2 screenPos)
    {
        //Step account
        float rayStrideHorizontal = MathF.Tan(fov / 2);
        float rayStrideVertical = rayStrideHorizontal * aspect;

        Vector3 lookDirection = Vector3.Normalize(lookLocation - origin);
        //Compute realitive vectors
        Vector3 realitiveRight = Vector3.Normalize(Vector3.Cross(cameraUp, lookDirection));
        Vector3 realitiveUp = Vector3.Normalize(Vector3.Cross(lookDirection, realitiveRight));

        Vector3 direction = Vector3.Normalize(lookDirection + ((realitiveRight * screenPos.X) - (realitiveUp * screenPos.Y)));

        return GetObjectHit(models, origin, direction);
    }

    public static RaycastHit? GetObjectHit(List<Model> models, Vector3 origin, Vector3 direction)
    {
        foreach (Model model in models)
        {
            if (!HasHitBoundingBox(model, origin, direction)) continue;

            foreach (var value in model.AllIndicies())
            {
                RaycastHit? hit = CheckForHit(model, value, origin, direction);
                if(hit != null)
                {
                    //TODO: translate the origin/ray direction based on model transformations.

                    hit.Model = model;
                    hit.Face = model.TriangleToFaceMapping[value];
                    return hit;//For now, report any hit triangle.
                }
            }
        }
        return null;
    }
}
