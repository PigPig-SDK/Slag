using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public static class Raycast
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

    public static RaycastHit? GetObjectHitScreenLocation(List<Model> models, Vector3 origin, Vector3 cameraUp, Vector3 lookLocation, float aspect, float fov, Vector2 screenPos, Vector2 screenSize)
    {
        /*
         * The following code is based on my raytracers screen to world calculations.
         * 
         * Converts a screen coordinate to valid raycast.
         */

        //Step account
        float strideVertical = MathF.Tan(fov / 2);//vertical FOV...
        float strayHorizontal = strideVertical * aspect;//Horizontal FOV depending on aspect ratio.

        Vector3 lookDirection = Vector3.Normalize(lookLocation - origin);
        //Compute realitive vectors
        Vector3 realitiveRight = Vector3.Normalize(Vector3.Cross(cameraUp, lookDirection));
        Vector3 realitiveUp = Vector3.Normalize(Vector3.Cross(lookDirection, realitiveRight));

        float stepX = strayHorizontal * 2.0f / (float)screenSize.X;
        float stepY = strideVertical * 2.0f / (float)screenSize.Y;
        Vector3 direction = Vector3.Normalize(lookDirection - (realitiveRight * (stepX * (screenPos.X - screenSize.X / 2)) + (realitiveUp * (stepY * (screenPos.Y - screenSize.Y/ 2)))));

        return ComputeRaycastHit(models, origin, direction);
    }

    public static RaycastHit? ComputeRaycastHit(List<Model> models, Vector3 origin, Vector3 direction)
    {
        Vector3 orignPure = origin;

        RaycastHit? hit = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Model model in models)
        {
            origin = orignPure - model.Position;


            if (!HasHitBoundingBox(model, origin, direction)) continue;

            foreach (var value in model.AllIndicies())
            {
                //TODO: translate the origin/ray direction based on model transformations.
                RaycastHit? hitTemp = CheckForHit(model, value, origin, direction);
                if(hitTemp != null)
                {
                    float distanceCheck = Vector3.Distance(origin, hitTemp.HitPoint!.Value);
                    if (distanceCheck > closestDistance) continue;
                    closestDistance = distanceCheck;

                    hitTemp.Model = model;
                    hitTemp.Face = model.TriangleToFaceMapping[value];
                    hit = hitTemp;
                }
            }
        }
        //Debug
        if(hit != null)
        {
            SceneHierarchy.Instance.AddModel(ModelPrefabs.DebugTriangleLine(origin, hit.HitPoint!.Value));
        }
        return hit;
    }
}
