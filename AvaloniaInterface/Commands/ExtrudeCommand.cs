using Avalonia.Input;
using Models;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private static (uint, uint, uint, uint) SortFour(uint a, uint b, uint c, uint d)
    {
        if (a > b) (a, b) = (b, a);
        if (c > d) (c, d) = (d, c);
        if (a > c) (a, c) = (c, a);
        if (b > d) (b, d) = (d, b);
        if (b > c) (b, c) = (c, b);

        return (a, b, c, d);
    }
    public void Extrude(Model model, HashSet<uint> selectedIndicies) 
    {
        Dictionary<uint, uint> cloneMapping = [];
        HashSet<(uint, uint, uint, uint)> faceMap = [];

        foreach (uint index in selectedIndicies)
        {
            //Add clone if possible
            if (!cloneMapping.TryGetValue(index, out uint cloneIndex))
            {
                Vertex vertex = model.GetVertex(index);
                cloneIndex = model.AddVertex(new(vertex), UpdateType.None);
                cloneMapping.Add(index, cloneIndex);
            }
            model.AddEdge(new(index, cloneIndex), UpdateType.None);//Discards diplicates automatically
            foreach(uint neighbor in model.VertexEdgeMap[index].ToArray())
            {
                if (!selectedIndicies.Contains(neighbor)) continue;

                //Add clone if possible
                if(!cloneMapping.TryGetValue(neighbor, out uint neighborCloneIndex))
                {
                    Vertex neighborVertex = model.GetVertex(neighbor);
                    neighborCloneIndex = model.AddVertex(new(neighborVertex), UpdateType.None);
                    cloneMapping.Add(neighbor, neighborCloneIndex);
                }

                var sorted = SortFour(index, neighbor, neighborCloneIndex, cloneIndex);
                //Double check if face data exists before adding it.
                if (!faceMap.Contains(sorted))
                {
                    faceMap.Add(sorted);
                    model.AddFaceUpdate(UpdateType.None, index, neighbor, neighborCloneIndex, cloneIndex);//Adds edges automatically.
                }
            }
        }
        SelectionManager.Instance.ClearSelection();
        HashSet<object> selectionEnd = [];
        foreach(uint indexOut in cloneMapping.Values)
        {
            selectionEnd.Add(indexOut);
        }
        SelectionManager.Instance.SetSelection(selectionEnd);
        model.UpdateAllComponents(UpdateType.Membership, null);
        model.UpdateAllComponents(UpdateType.Selection, null);
    }
}
