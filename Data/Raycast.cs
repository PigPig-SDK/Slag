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

        return false;
    }

    public static RaycastHit? GetObjectHit(List<Model> models, Vector3 origin, Vector3 direction)
    {

        foreach (Model model in models)
        {

        }

        return null;
    }
}
