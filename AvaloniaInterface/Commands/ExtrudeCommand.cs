using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Collections.Generic;

namespace OpenglAvaloniaTest.Commands;
public class ExtrudeCommand : MementoCommand
{
    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        SelectionManager sm = SelectionManager.Instance;
        if (sm.CurrentModel is null) return CommandState.Discard;

        CreateState();
        
        Extrude(sm.CurrentModel, [.. sm.GetSelection<uint>()]);

        return CommandState.Finished;
    }

    public void Extrude(Model model, HashSet<uint> indicies) 
    {
        Dictionary<uint, uint> cloneMapping = [];

        foreach (uint index in indicies)
        {
            Console.WriteLine("primary" + index);
            //Add clone if possible
            if (!cloneMapping.TryGetValue(index, out uint cloneIndex))
            {
                Vertex vertex = model.GetVertex(index);
                cloneIndex = model.AddVertex(new(vertex), UpdateType.None);
                cloneMapping.Add(index, cloneIndex);
            }
            model.AddEdge(new(index, cloneIndex), UpdateType.None);//Discards diplicates automatically
            Console.WriteLine("Length : " + model.VertexEdgeMap[index].Count);
            foreach(uint neighbor in model.VertexEdgeMap[index])
            {
                Console.WriteLine("neighbor" + neighbor);
                if (!indicies.Contains(neighbor)) continue;

                //Add clone if possible
                if(!cloneMapping.TryGetValue(index, out uint neighborCloneIndex))
                {
                    Vertex neighborVertex = model.GetVertex(neighbor);
                    neighborCloneIndex = model.AddVertex(new(neighborVertex), UpdateType.None);
                    cloneMapping.Add(neighbor, neighborCloneIndex);
                    //Generate face
                }
                model.AddFaceUpdate(UpdateType.None, neighborCloneIndex, index, neighbor, cloneIndex);//Adds edges automatically.
            }
        }
        SelectionManager.Instance.ClearSelection();
        SelectionManager.Instance.SetSelection([..cloneMapping.Values]);
        model.UpdateAllComponents(UpdateType.Membership, null);
        model.UpdateAllComponents(UpdateType.Selection, null);
    }
}
