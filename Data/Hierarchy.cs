using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class Hierarchy
{
    public static Hierarchy Instance = new();

    public List<Model> Models = [Model.InstanceBasicTriangle()];

}
