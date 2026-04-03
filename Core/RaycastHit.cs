using OpenTK.Mathematics;

namespace Core;

public class RaycastHit
{
    public Model Model { get; set; }
    public Face Face { get; set; }
    public (uint v1, uint v2, uint v3) TriangleIndicies { get; set; }
    public Vector3 BarycentricPoint { get; set; }
    public Vector3 HitPoint { get; set; }

    public RaycastHit(Model model, Face face, Vector3 hitPoint, Vector3 barycentricPoint, (uint v1, uint v2, uint v3) triangleIndicies)
    {
        Model = model;
        Face = face;
        HitPoint = hitPoint;
        BarycentricPoint = barycentricPoint;
        this.TriangleIndicies = triangleIndicies;
    }

    public override string ToString()
    {
        return $"Model name {Model?.ObjectName} : Face {Face} : HitPoint {HitPoint} : Barry {BarycentricPoint}";
    }
}
