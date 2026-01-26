using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

public class ModelSelection : ModelComponent
{
    private HashSet<uint> SelectedIndicies = [];

    /// <summary>
    /// First parameter is the index, second parameter is true if selected, false if deselected
    /// </summary>
    public event Action<uint, bool, ModelUpdateType>? OnSelectionChanged;

    public event Action<ModelUpdateType>? OnSelectionMassUpdate;

    public ModelSelection(Model model)
    {
        this.model = model;
    }

    public void SelectIndex(uint index, ModelUpdateType updateInfo = ModelUpdateType.Vertex)
    {
        SelectedIndicies.Add(index);
        OnSelectionChanged?.Invoke(index, true, updateInfo);
    }

    public void DeselectAll(ModelUpdateType updateInfo = ModelUpdateType.Vertex)
    {
        SelectedIndicies.Clear();
        OnSelectionMassUpdate?.Invoke(updateInfo);
    }

    public void BroadcastMassUpdate(ModelUpdateType updateInfo) => OnSelectionMassUpdate?.Invoke(updateInfo);

    public void DeselectIndex(uint index, ModelUpdateType updateInfo = ModelUpdateType.Vertex)
    {
        SelectedIndicies.Remove(index);
        OnSelectionChanged?.Invoke(index, false, updateInfo);
    }

    public override void Dispose() { }

    public override void OnModelUpdate(Model model, ModelUpdateType info, object data)
    {
        

    }

    public override void OnAddedToModel(Model model) { }

    public bool IsVertexSelected(uint i) => SelectedIndicies.Contains(i);
}
