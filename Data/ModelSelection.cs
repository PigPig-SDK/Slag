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
    public event Action<uint, bool>? OnSelectionChanged;

    public event Action? OnSelectionMassUpdate;

    public ModelSelection(Model model)
    {
        this.model = model;
    }

    public void SelectIndex(uint index)
    {
        Console.WriteLine("Select index " + index);
        SelectedIndicies.Clear();
        SelectedIndicies.Add(index);
        OnSelectionChanged?.Invoke(index, true);
    }

    public void DeselectIndex(uint index)
    {
        SelectedIndicies.Remove(index);
        OnSelectionChanged?.Invoke(index, false);
    }

    public override void Dispose() { }

    public override void OnModelUpdate(Model model, ModelUpdateType info, object data)
    {
        

    }

    public override void OnAddedToModel(Model model) { }

    public bool IsVertexSelected(uint i) => SelectedIndicies.Contains(i);
}
