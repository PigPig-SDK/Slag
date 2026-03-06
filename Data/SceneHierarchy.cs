namespace Models;

public class SceneHierarchy
{
    public static SceneHierarchy Instance = new();

    public IReadOnlyDictionary<HierarchyType, List<Model>> HierarchyCategories => _hierarchyCategories;
    private Dictionary<HierarchyType, List<Model>> _hierarchyCategories = new()
    {
        { HierarchyType.Model, [ModelPrefabs.Cube(), ModelPrefabs.Plane(10)] },
        { HierarchyType.Tool, [ModelPrefabs.AxisTriad(), ModelPrefabs.SelectionInstance()] },
    };

    public event Action<HierarchyType,Model>? OnModelAdded;

    public event Action<HierarchyType, Model>? OnModelRemoved;

    public void AddModel(HierarchyType hierarchyType, Model model)
    {
        model.hierarchyType = hierarchyType;
        _hierarchyCategories[hierarchyType].Add(model);
        OnModelAdded?.Invoke(hierarchyType,model);
    }

    public void RemoveModel(HierarchyType hierarchyType, Model model)
    {
        model.Dispose();
        _hierarchyCategories[hierarchyType].Remove(model);
        OnModelRemoved?.Invoke(hierarchyType,model);
    }

    public IEnumerable<Model> GetModels(HierarchyType hierarchyType)
    {
        foreach(HierarchyType hierarchy in _hierarchyCategories.Keys)
        {
            if(hierarchyType.HasFlag(hierarchy))
            {
                foreach(Model model in _hierarchyCategories[hierarchy])
                {
                    yield return model;
                }
            }
        }
    }
}
