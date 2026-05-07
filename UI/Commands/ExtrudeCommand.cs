using Avalonia.Input;
using Avalonia.Styling;
using AvaloniaEdit.Utils;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.ViewModels;


namespace UI.Commands;
public class ExtrudeCommand : MementoCommand
{
    public override string Name => "Extrude";

    public override string Description => "From selection";

    public override bool DisplayToolText => false;

    public override string IconSource => "avares://Slag/Assets/icons/extrude.png";
    public override bool AllowInMeshMode => false;
    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        SelectionManager sm = SelectionManager.Instance;
        if (sm.CurrentModel is null) return CommandState.Discard;
        SelectionComponent? selection = sm.CurrentModel.GetComponent<SelectionComponent>();
        if(selection is null) return CommandState.Discard;
        CreateState();

        HashSet<uint> selectedIndices = [];
        HashSet<Edge> edgeWhiteList = [];
        HashSet<object> endingSelection = [..selection.SelectionBucket().ToList()];

        //If faces are selected, find border
        AddFaceBorders(selection, ref selectedIndices, ref edgeWhiteList);

        selectedIndices.AddRange(selection.SelectedIndices);

        Extrude(sm.CurrentModel, selectedIndices, edgeWhiteList, selection.GetCenter(), out Dictionary<uint, uint> cloneMap);

        AdjustConnectedFaces(selection, sm.CurrentModel, cloneMap, edgeWhiteList, out Dictionary<Face,Face> faceCloneMap);

        //Finalize
        selection.DeselectAll();
        foreach (object selected in endingSelection)
        {
            if(selected is uint index)
            {
                if (cloneMap.TryGetValue(index, out uint value))
                    selection.SelectIndex(value, UpdateType.Ignore);//Select the clone instead.
            }
            else if(selected is Face face)
            {
                if(faceCloneMap.TryGetValue(face, out Face? value))
                    selection.SelectFace(value, UpdateType.Ignore);//Select the clone instead.
            }
        }

        selection.Model.UpdateAllComponents(UpdateType.Membership);
        selection.BroadcastMassUpdate(UpdateType.Selection);
        return CommandState.Finished;
    }
    /// <summary>
    /// This function selects the outline of a set of faces.
    /// Say you select 10 faces that are contiguous,
    /// Edges that partake in two or more faces will not be added to the selected indices.
    /// </summary>
    /// <param name="selection"> The selected objects selection component</param>
    /// <param name="selectedIndices"> The selected indices that will be modified</param>
    private static void AddFaceBorders(SelectionComponent selection, ref HashSet<uint> selectedIndices, ref HashSet<Edge> edgeWhiteList)
    {
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
                selectedIndices.Add(edge.Vertex1);
                selectedIndices.Add(edge.Vertex2);
                edgeWhiteList.Add(edge);
            }
        }
    }
    private static void AdjustConnectedFaces(SelectionComponent selection, Model model, 
        Dictionary<uint, uint> cloneMap, HashSet<Edge> edgeWhiteList, 
        out Dictionary<Face, Face> faceMap)
    {
        faceMap = [];
        HashSet<Edge> edgesToRemove = [];

        foreach(Face face in selection.SelectedFaces)
        {
            //Remove any edges which are entirely contained by the clone set
            foreach (Edge edge in face.Edges)
            {
                if (edgeWhiteList.Contains(edge)) continue;//Ignore side edges.

                if (cloneMap.ContainsKey(edge.Vertex1) || cloneMap.ContainsKey(edge.Vertex2))
                    edgesToRemove.Add(edge);
            }
        }

        foreach(Face face in selection.SelectedFaces.ToArray())//Will be modified. ToArray required
        {
            bool containsClones = false;
            foreach(uint index in face.Indices)
            {
                if(cloneMap.ContainsKey(index))
                {
                    containsClones = true;
                    break;
                }
            }
            if (!containsClones) break;

            // Reuse allocation of old face.
            var newFaceIndices = face.Indices;



            //Delete existing polygon
            model.RemoveFace(face);

            for(int i = 0; i < newFaceIndices.Count; i++)
            {
                if(cloneMap.ContainsKey((uint)newFaceIndices[i]))
                    newFaceIndices[i] = cloneMap[newFaceIndices[i]];//Remap to new index.
            }
            //Throw back into mesh
            Face newFace = new Face(newFaceIndices);
            model.AddFace(newFace, UpdateType.Ignore);
            faceMap.Add(face, newFace);
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
    public static void Extrude(Model model, HashSet<uint> selectedIndices, HashSet<Edge> edgeWhiteList, Vector3 selectionOrigin, out Dictionary<uint, uint> cloneMapping) 
    {
        cloneMapping = [];
        HashSet<(uint, uint, uint, uint)> faceMap = [];

        foreach (uint index in selectedIndices)
        {
            //Add clone if possible
            if (!cloneMapping.TryGetValue(index, out uint cloneIndex))
            {
                Vertex vertex = model.GetVertex(index);
                cloneIndex = model.AddVertex(new(vertex), UpdateType.None);
                cloneMapping.Add(index, cloneIndex);
            }
            model.AddEdge(new(index, cloneIndex), UpdateType.None);//Discards diplicates automatically
            foreach (uint neighbor in model.VertexEdgeMap[(int)index].ToArray())
            {
                //Ignore non selected edges
                if (!selectedIndices.Contains(neighbor)) continue;
                //Ignore edges that are not whitelisted.
                Edge hashEdge = new(index, neighbor);
                if (!edgeWhiteList.TryGetValue(hashEdge, out Edge? edge)) continue;
                
                //Add clone if possible
                if(!cloneMapping.TryGetValue(neighbor, out uint neighborCloneIndex))
                {
                    Vertex neighborVertex = model.GetVertex(neighbor);
                    neighborCloneIndex = model.AddVertex(new(neighborVertex), UpdateType.None);
                    cloneMapping.Add(neighbor, neighborCloneIndex);
                }

                var sorted = SortFour(index, neighbor, neighborCloneIndex, cloneIndex);
                //Double check if face data exists before adding it.
                if (faceMap.Add(sorted))
                {
                    Vector3 cloneLocation = model.GetVertex(cloneIndex).Position;
                    Vector3 fakecloneLocation =(cloneLocation + (cloneLocation - selectionOrigin).Normalized() * 0.1f);//Nudge toward center.
                    //Compute normal for new face
                    Vector3 e1 = model.GetVertex(neighbor).Position - model.GetVertex(index).Position;
                    Vector3 e2 = fakecloneLocation - model.GetVertex(index).Position;

                    Vector3 newFaceNormalAssumed = Vector3.Cross(e1, e2).Normalized();
                    Vector3 oldEdgeAssumedNormal = edge.GetAssumedNormal();

                    float dotProduct = Vector3.Dot(newFaceNormalAssumed, oldEdgeAssumedNormal);

                    if(dotProduct > 0)
                        model.AddFaceWithMembership(UpdateType.None, cloneIndex, neighborCloneIndex, neighbor, index);//Flipped
                    else
                        model.AddFaceWithMembership(UpdateType.None, index, neighbor, neighborCloneIndex, cloneIndex);//Not flipped
                }
            }
        }
    }
}
