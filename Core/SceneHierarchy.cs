namespace Core;

public class SceneHierarchy
{
    public static SceneHierarchy Instance { get; } = new();

    public const string XPlaneName = "XPlane";
    public const string YPlaneName = "YPlane";
    public const string ZPlaneName = "ZPlane";

    public IReadOnlyDictionary<HierarchyType, List<Model>> HierarchyCategories => _hierarchyCategories;
    private Dictionary<HierarchyType, List<Model>> _hierarchyCategories = new()
    {
        { HierarchyType.Model, [ModelPrefabs.Cube()] },
        { HierarchyType.EditVisualizer, 
        [   ModelPrefabs.Plane(40, XPlaneName),
            ModelPrefabs.Plane(40, YPlaneName),
            ModelPrefabs.Plane(40, ZPlaneName)] },
        { HierarchyType.Tool, [
            ModelPrefabs.AxisTriad(),
            ModelPrefabs.SelectionInstance()]},
    };

    public event Action<HierarchyType,Model>? OnModelAdded;

    public event Action<HierarchyType, Model>? OnModelRemoved;

    public void AddModel(HierarchyType hierarchyType, Model model)
    {
        model.HierarchyType = hierarchyType;
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
