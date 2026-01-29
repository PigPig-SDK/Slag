using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    public Dictionary<HierarchyType, List<Model>> HierarchyCategories { get; private set; } = new()
    {
        { HierarchyType.Model, [ModelPrefabs.Cube()] },
        { HierarchyType.Tool, [ModelPrefabs.AxisTriad()] },
    };

    public event Action<HierarchyType,Model>? OnModelAdded;

    public event Action<HierarchyType, Model>? OnModelRemoved;

    public void AddModel(HierarchyType hierarchyType, Model model)
    {
        HierarchyCategories[hierarchyType].Add(model);
        OnModelAdded?.Invoke(hierarchyType,model);
    }

    public void RemoveModel(HierarchyType hierarchyType, Model model)
    {
        model.Dispose();
        HierarchyCategories[hierarchyType].Remove(model);
        OnModelRemoved?.Invoke(hierarchyType,model);
    }

    public IEnumerable<Model> GetModels(HierarchyType hierarchyType)
    {
        foreach(HierarchyType hierarchy in HierarchyCategories.Keys)
        {
            if(hierarchyType.HasFlag(hierarchy))
            {
                foreach(Model model in HierarchyCategories[hierarchy])
                {
                    yield return model;
                }
            }
        }
    }
}
