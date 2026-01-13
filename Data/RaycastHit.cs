using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class RaycastHit
{
    public Model? Model= null;
    public Face? Face = null;
    public Vector3? HitPoint;
    public Vector3? BarycentricPoint;

    public override string ToString()
    {
        return $"Model name {Model?.ObjectName} : Face {Face} : HitPoint {HitPoint} : Barry {BarycentricPoint}";
    }
}
