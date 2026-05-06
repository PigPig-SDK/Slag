using Core;
using System.Collections.Generic;

namespace UI.ViewModels;

public class ClipBoard
{
    public static ClipBoard Instance { get; private set; } = new();

    private readonly List<Model> models = [];

    public void Copy(IReadOnlyCollection<Model> toClone)
    {
        models.Clear();
        foreach(Model model in toClone)
        {
            var clone = model.Clone();
            models.Add(clone);
        }
    }
    public void Paste()
    {
        SceneHierarchy hierarchy = SceneHierarchy.Instance;
        foreach (Model model in models)
        {
            hierarchy.AddModel(HierarchyType.Model, model.Clone());
        }
    }
}
