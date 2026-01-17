using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    private List<Model> Models { get; set; } = [ModelPrefabs.InstanceCone(100,3,1)];

    private List<Model> Tools { get; set; } = [ModelPrefabs.InstanceAxisTriad()];

    public event Action<Model>? OnModelAdded;

    public event Action<Model>? OnModelRemoved;

    public void AddModel(Model model)
    {
        Models.Add(model);
        OnModelAdded?.Invoke(model);
    }

    public void RemoveModel(Model model)
    {
        model.Dispose();
        Models.Remove(model);
        OnModelRemoved?.Invoke(model);
    }

    public IEnumerable<Model> SceneModels()
    {
        foreach (var model in Models)
        {
            yield return model;
        }
    }

    public IEnumerable<Model> SceneTools()
    {
        foreach (var model in Tools)
        {
            yield return model;
        }
    }

    public IEnumerable<Model> AllModels()
    {
        foreach (var model in Models)
        {
            yield return model;
        }
        foreach (var model in Tools)
        { 
            yield return model;
        }
    }

}
