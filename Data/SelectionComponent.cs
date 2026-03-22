using Models;
using OpenTK.Mathematics;

namespace OpenglAvaloniaTest.ViewModels;

public class SelectionComponent : ModelComponent
{
    private HashSet<uint> SelectedIndicies = [];

    /// <summary>
    /// First parameter is the index, second parameter is true if selected, false if deselected
    /// </summary>
    public event Action<uint, bool, UpdateType>? OnSelectionChanged;

    public event Action<UpdateType>? OnSelectionMassUpdate;

    public void SelectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        SelectedIndicies.Add(index);
        OnSelectionChanged?.Invoke(index, true, updateInfo);
    }

    public void DeselectAll(UpdateType updateInfo = UpdateType.Selection)
    {
        SelectedIndicies.Clear();
        OnSelectionMassUpdate?.Invoke(updateInfo);
    }

    public void BroadcastMassUpdate(UpdateType updateInfo) => OnSelectionMassUpdate?.Invoke(updateInfo);

    public void DeselectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        SelectedIndicies.Remove(index);
        OnSelectionChanged?.Invoke(index, false, updateInfo);
    }

    public override void Dispose() { }

    public override void OnModelUpdate(Model model, UpdateType info)
    {

    }

    public override void OnAddedToModel(Model model) { }

    public bool IsVertexSelected(uint i) => SelectedIndicies.Contains(i);

    public static bool BindComponent(Model model)
    {
        if (!model.HasComponent(typeof(SelectionComponent)))
            model.AddComponent<SelectionComponent>(new SelectionComponent());
        return true;
    }

    public IEnumerable<uint> SelectionIndicies()
    {
        foreach(uint item in SelectedIndicies) yield return item;
    }

    public Vector3 GetWorldCenter()
    {
        return Model.TransformPointByModelMatrix(GetCenter());
    }

    public Vector3 GetCenter()
    {
        if(SelectedIndicies.Count == 0)
            return Vector3.Zero;

        Vector3 sum = Vector3.Zero;

        foreach (uint ind in SelectedIndicies)
        {
            Vertex v = Model.GetVertex(ind);
            sum += v.Position;
        }

        return sum/SelectedIndicies.Count;
    }
}
