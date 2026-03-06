using OpenTK.Mathematics;

namespace Models;

public class RaycastHit
{
    public Model Model;
    public Face Face;
    public (uint v1, uint v2, uint v3) triangleIndicies;
    public Vector3 BarycentricPoint;
    public Vector3 HitPoint;

    public RaycastHit(Model model, Face face, Vector3 hitPoint, Vector3 barycentricPoint, (uint v1, uint v2, uint v3) triangleIndicies)
    {
        Model = model;
        Face = face;
        HitPoint = hitPoint;
        BarycentricPoint = barycentricPoint;
        this.triangleIndicies = triangleIndicies;
    }

    public override string ToString()
    {
        return $"Model name {Model?.ObjectName} : Face {Face} : HitPoint {HitPoint} : Barry {BarycentricPoint}";
    }
}
