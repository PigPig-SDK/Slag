using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    public List<Model> Models = [ModelPrefabs.InstanceBasicCube()];

    public List<Model> ToolModels = [ModelPrefabs.InstanceAxisTriad()];

}
