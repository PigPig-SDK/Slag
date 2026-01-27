using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    private List<Model> Models { get; set; } = [ModelPrefabs.Cube()];

    private List<Model> Tools { get; set; } = [ModelPrefabs.AxisTriad()];

    public event Action<Model>? OnModelAdded;

    public event Action<Model>? OnModelRemoved;

    public void AddModel(Model model)
    {
        Models.Add(model);
        OnModelAdded?.Invoke(model);
    }

    public void AddToolModel(Model model)
    {
        Tools.Add(model);
    }

    public void RemoveModel(Model model)
    {
        model.Dispose();
        Models.Remove(model);
        OnModelRemoved?.Invoke(model);
    }

    public IEnumerable<Model> SceneModels() => Models;

    public IEnumerable<Model> SceneTools() => Tools;

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
