using OpenTK.Mathematics;

namespace Core;

public static class Raycast
{
    private const float LineSelectionDistance = 0.01f;
    private const float VertexSelectionDistance = 0.001f;


    public static bool HasHitBoundingBox(Model model, Vector3 origin, Vector3 direction)
    {
        return true;
    }

    private static (Vector3 hitPoint, Vector3 barryCoords)? CheckForHit(Model m, (uint e1, uint e2, uint e3) incidies, Vector3 origin, Vector3 direction)
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
            return (origin + direction * t, new Vector3(u, v, 1 - (u + v)));
        }
        else
            return null;
    }

    public static RaycastHit? GetObjectHitScreenLocation(IEnumerable<Model> models, Vector3 origin, Vector3 cameraUp, Vector3 lookLocation, float aspect, float fov, Vector2 screenPos, Vector2 screenSize)
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

        return GetRaycastHit(models, origin, direction);
    }

    public static EdgeHit? GetEdgeHit(IEnumerable<Model> models, Vector2 glScreenPos, Matrix4 cameraMatrix, Vector3 cameraCenter)
    {
        /**
         * The following code maps the edge to 2d space, creating a line.
         * The selection is determined by a small tangent line being created at the cursor.
         * The selection line and the edge line is then intersection tested.
         * 
         * To solve Z problems, the closest intersecting line is picked.
         * 'Close' is determined by the lines center. Not perfect, but it works enough for me to use it.
         */
        EdgeHit? hit = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Model model in models)
        {
            if (model.Hidden) continue;//Do not scan against hidden models.

            foreach (Edge edge in model.Edges)
            {
                //Get verts
                Vector3 start = model.GetVertex(edge.Vertex1).Position;
                Vector3 end = model.GetVertex(edge.Vertex2).Position;

                //Vertex to camera space
                Vector4 startScreenSpace = new Vector4(start, 1.0f) * model.GetModelMatrix() * cameraMatrix;
                startScreenSpace /= startScreenSpace.W;
                Vector4 endScreenSpace = new Vector4(end, 1.0f) * model.GetModelMatrix() * cameraMatrix;
                endScreenSpace /= endScreenSpace.W;

                //Compute tangent
                Vector2 tangentDir = (endScreenSpace.Xy - startScreenSpace.Xy);
                tangentDir = new Vector2(tangentDir.Y, -tangentDir.X);
                tangentDir.Normalize();
                tangentDir *= LineSelectionDistance;

                //Create user selection line
                Vector2 selectStart = glScreenPos + tangentDir;
                Vector2 selectEnd = glScreenPos - tangentDir;

                //Compute line intersection
                if (LineIntersection.FindSegmentIntersection(startScreenSpace.Xy, endScreenSpace.Xy, selectStart, selectEnd, out Vector2 intersectionPoint))
                {
                    //Compute center to see if its the closest to the camera
                    Vector3 lineCenter = (start + end) / 2f;

                    float distance = Vector3.DistanceSquared(cameraCenter, lineCenter);
                    if (distance > closestDistance) continue;
                    closestDistance = distance;
                    hit = new EdgeHit(edge, model);
                }
            }
        }
        return hit;
    }

    /// <summary>
    /// Gets the closest vertex to the current cursor.
    /// </summary>
    /// <param name="models">The models we are set to test against</param>
    /// <param name="glScreenPos"></param>
    /// <param name="cameraMatrix"></param>
    /// <returns></returns>
    public static VertexHit? GetVertexHit(IEnumerable<Model> models, Vector2 glScreenPos, Matrix4 cameraMatrix)
    {
        VertexHit? hit = null;
        float closestZDistance = float.PositiveInfinity;
        Vector3 screenposRealitive = new Vector3(glScreenPos.X, glScreenPos.Y, 0);

        foreach (Model model in models)
        {
            if (model.Hidden) continue;//Do not scan against hidden models.

            uint index = 0;
            foreach (Vertex v in model.Verticies)
            {
                //Vertex to camera space
                Vector4 pos = new Vector4(v.Position, 1.0f);
                pos = pos * model.GetModelMatrix() * cameraMatrix;
                Vector3 vertexCamera = (pos.Xyz)/pos.W;

                //Takes Z into account as well to avoid selecting vertices behind other ones.
                float distanceCheck = Vector2.DistanceSquared(screenposRealitive.Xy, vertexCamera.Xy);
                float distanceZ = vertexCamera.Z - screenposRealitive.Z;

                if (distanceCheck < VertexSelectionDistance  && distanceZ < closestZDistance)
                {
                    closestZDistance = distanceZ;
                    hit = new VertexHit(model, index, distanceCheck);
                }
                index++;
            }
        }
        return hit;
    }

    public static RaycastHit? GetRaycastHit(IEnumerable<Model> models, Vector3 origin, Vector3 direction)
    {
        RaycastHit? hit = null; 
        float closestDistance = float.PositiveInfinity;
        foreach (Model model in models)
        {
            if(model.Hidden) continue;//Do not scan against hidden models.

            Matrix4 modelInv = model.GetModelMatrix().Inverted();
            Vector3 originHomo = ((new Vector4(origin, 1.0f) * modelInv)).Xyz;
            Vector3 directionHomo =  ((new Vector4(direction, 0.0f) * modelInv)).Xyz.Normalized();

            if (!HasHitBoundingBox(model, originHomo, directionHomo)) continue;

            foreach (var triangleIndicies in model.AllTrianglesAsIndicies())
            {
                (Vector3 hitPoint, Vector3 barryCoords)? hitPoints = CheckForHit(model, triangleIndicies, originHomo, directionHomo);
                if(hitPoints != null)
                {
                    //Translate hitpoint by model transformation
                    Vector3 hitPoint = (new Vector4(hitPoints!.Value.hitPoint, 1.0f) * model.GetModelMatrix()).Xyz;

                    float distanceCheck = Vector3.DistanceSquared(originHomo, hitPoint);
                    if (distanceCheck > closestDistance) continue;
                    closestDistance = distanceCheck;
                    hit = new (model, model.TriangleToFaceMapping[triangleIndicies], hitPoint, hitPoints!.Value.barryCoords, triangleIndicies);
                }
            }
        }

        return hit;
    }
}
