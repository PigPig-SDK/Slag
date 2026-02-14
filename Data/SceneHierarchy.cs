using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    public IReadOnlyDictionary<HierarchyType, List<Model>> HierarchyCategories => _HierarchyCategories;
    private Dictionary<HierarchyType, List<Model>> _HierarchyCategories = new()
    {
        { HierarchyType.Model, [ModelPrefabs.DebugEdges()] },
        { HierarchyType.Tool, [ModelPrefabs.AxisTriad(), ModelPrefabs.SelectionInstance()] },
    };

    public event Action<HierarchyType,Model>? OnModelAdded;

    public event Action<HierarchyType, Model>? OnModelRemoved;

    public void AddModel(HierarchyType hierarchyType, Model model)
    {
        model.hierarchyType = hierarchyType;
        _HierarchyCategories[hierarchyType].Add(model);
        OnModelAdded?.Invoke(hierarchyType,model);
    }

    public void RemoveModel(HierarchyType hierarchyType, Model model)
    {
        model.Dispose();
        _HierarchyCategories[hierarchyType].Remove(model);
        OnModelRemoved?.Invoke(hierarchyType,model);
    }

    public IEnumerable<Model> GetModels(HierarchyType hierarchyType)
    {
        foreach(HierarchyType hierarchy in _HierarchyCategories.Keys)
        {
            if(hierarchyType.HasFlag(hierarchy))
            {
                foreach(Model model in _HierarchyCategories[hierarchy])
                {
                    yield return model;
                }
            }
        }
    }
}
