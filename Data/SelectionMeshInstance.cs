using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class SelectionMeshInstance : ModelComponent
{
    public static SelectionMeshInstance? Instance { get; set; }

    public SelectionMeshInstance() 
    {
        if(Instance is not null)
        {
            throw new InvalidOperationException("Constructed more than one SelectionMeshInstance");
        }

        Instance = this;
    }

    public override void Dispose() { }
    public override void OnAddedToModel(Model model) { }
    public override void OnModelUpdate(Model model, UpdateType info, object? data) { }

    public void SelectFace(Face face)
    {
        
    }

    public void DeselectFace(Face face)
    {

    }
}
