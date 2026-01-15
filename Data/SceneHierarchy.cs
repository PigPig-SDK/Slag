using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    public List<Model> Models { get; private set; } = [ModelPrefabs.InstanceCone(100,3,1)];

    public List<Model> ToolModels { get; private set; } = [ModelPrefabs.InstanceAxisTriad()];

    public event Action<Model> OnModelAdded;

    public void AddModel(Model model)
    {
        Models.Add(model);
        OnModelAdded?.Invoke(model);
    }

    public IEnumerable<Model> AllModels()
    {
        foreach (var model in Models)
        {
            yield return model;
        }
        foreach (var model in ToolModels)
        { 
            yield return model;
        }
    }

}
