using OpenTK.Mathematics;
using System;

namespace Models;

internal class BVHNode
{
    public BVHNode? left, right, parent;
    public List<uint>? Indicies = null;

    public Vector3 Start;
    public Vector3 End;

    private Model? _visualizer = null;

    public void AddIndex(Model model, uint index, bool regenParents)
    {
        if(Indicies == null) Indicies = new List<uint>();
        Indicies.Add(index);
        if (!model.TryGetVertex(index, out Vertex? vertex)) return;
        RefitParents(vertex!.Value.Position);
    }

    public void RemoveIndex(Model model, uint id)
    {
        if (Indicies == null || Indicies.Count == 0)
            return;
        Indicies.Remove(id);
    }

    public bool Refit(Vector3 vertexLocation)
    {
        Vector3 newStart = Vector3.ComponentMin(Start, vertexLocation);
        Vector3 newEnd = Vector3.ComponentMax(End, vertexLocation);

        if (newStart == Start && newEnd == End) return false;
        //bounds are new
        Start = newStart;
        End = newEnd;
        return true;
    }

    public void RefitParents(Vector3 vertexLocation)
    {
        //inform parent of if it exists.
        if (Refit(vertexLocation))
            parent?.RefitParents(vertexLocation);
    }

    public void ComputeSize(Model model, List<uint> indicies, (int start, int size) range)
    {
        for(int i = range.start; i < range.start + range.size; i++)
        {
            if (!model.TryGetVertex(indicies[i], out Vertex? vertex)) continue;
            Refit(vertex!.Value.Position);
        }
        _visualizer = ModelPrefabs.BBoxVisualizer(Start, End);
        //SceneHierarchy.Instance.AddModel(HierarchyType.Tool, _visualizer);
    }
}
