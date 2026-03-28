using Avalonia.Input;
using Avalonia.Platform;
using AvaloniaEdit.Utils;
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
        SelectionComponent? selection = sm.CurrentModel.GetComponent<SelectionComponent>();
        if(selection is null) return CommandState.Discard;
        CreateState();

        HashSet<uint> selectedIndicies = [];
        HashSet<Edge> edgeWhiteList = [];

        //If faces are selected, find border
        AddFaceBorders(selection, ref selectedIndicies, ref edgeWhiteList);

        selectedIndicies.AddRange(selection.SelectedIndicies);

        Extrude(sm.CurrentModel, selectedIndicies, edgeWhiteList, out Dictionary<uint, uint> cloneMap);

        AdjustConnectedFaces(selection, sm.CurrentModel, cloneMap, edgeWhiteList);

        //Finalize
        SelectionManager.Instance.ClearSelection();

        HashSet<object> selectionEnd = [];
        foreach (uint indexOut in cloneMap.Values)
        {
            selectionEnd.Add(indexOut);
        }
        SelectionManager.Instance.SetSelection(selectionEnd);

        //Redisplay.
        sm.CurrentModel.UpdateAllComponents(UpdateType.Membership);
        sm.CurrentModel.UpdateAllComponents(UpdateType.Selection);

        return CommandState.Finished;
    }
    /// <summary>
    /// This function selects the outline of a set of faces.
    /// Say you select 10 faces that are contiguous,
    /// Edges that partake in two or more faces will not be added to the selected indicies.
    /// </summary>
    /// <param name="selection"> The selected objects selection component</param>
    /// <param name="selectedIndicies"> The selected indicies that will be modified</param>
    private void AddFaceBorders(SelectionComponent selection, ref HashSet<uint> selectedIndicies, ref HashSet<Edge> edgeWhiteList)
    {
        int total = selection.GetSelection<Edge>().Count();

        foreach(Edge edge in selection.GetSelection<Edge>())//All edges in our selection
        {
            int count = 0;
            foreach(Face face in edge.Faces)
            {
                if (selection.SelectedFaces.Contains(face))
                    count++;
            }
            //Edge is only seen once in face set or never.
            if(count <= 1)
            {
                selectedIndicies.Add(edge.Vertex1);
                selectedIndicies.Add(edge.Vertex2);
                edgeWhiteList.Add(edge);
            }
        }
    }
    private void AdjustConnectedFaces(SelectionComponent selection, Model model, Dictionary<uint, uint> cloneMap, HashSet<Edge> edgeWhiteList)
    {
        HashSet<Edge> edgesToRemove = [];

        foreach(Face face in selection.SelectedFaces)
        {
            //Remove any edges which are entirely contained by the clone set
            foreach (Edge edge in face.Edges)
            {
                if (edgeWhiteList.Contains(edge)) continue;//Ignore side edges.

                if (cloneMap.ContainsKey(edge.Vertex1) && cloneMap.ContainsKey(edge.Vertex2))
                    edgesToRemove.Add(edge);
            }
        }

        foreach(Face face in selection.SelectedFaces.ToArray())//Will be modified. ToArray required
        {
            bool containsClones = false;
            foreach(uint index in face.Indicies)
            {
                if(cloneMap.ContainsKey(index))
                {
                    containsClones = true;
                    break;
                }
            }
            if (!containsClones) break;

            // Reuse allocation of old face.
            var newFaceIndicies = face.Indicies;



            //Delete existing polygon
            model.RemoveFace(face);

            for(int i = 0; i < newFaceIndicies.Count; i++)
            {
                if(cloneMap.ContainsKey((uint)newFaceIndicies[i]))
                    newFaceIndicies[i] = cloneMap[newFaceIndicies[i]];//Remap to new index.
            }
            //Throw back into mesh
            model.AddFace(new Face(newFaceIndicies), UpdateType.Ignore);
        }

        //Remove 'floating' undesirable edges.
        foreach (Edge edge in edgesToRemove) model.RemoveEdge(edge, UpdateType.Ignore);
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
    public void Extrude(Model model, HashSet<uint> selectedIndicies, HashSet<Edge> edgeWhiteList, out Dictionary<uint, uint> cloneMapping) 
    {
        cloneMapping = [];
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
            foreach(uint neighbor in model.VertexEdgeMap[(int)index].ToArray())
            {
                //Ignore non selected edges
                if (!selectedIndicies.Contains(neighbor)) continue;
                //Ignore edges that are not whitelisted.
                if(!edgeWhiteList.Contains(new Edge(index,neighbor))) continue;

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
    }
}
