using Models;
using OpenTK.Mathematics;
using System;

namespace OpenglAvaloniaTest.ViewModels;

public class SelectionComponent : ModelComponent
{
    private HashSet<uint> _selectedIndicies = [];
    public IReadOnlySet<uint> SelectedIndicies => _selectedIndicies; 

    private HashSet<Face> _selectedFaces = [];
    public IReadOnlySet<Face> SelectedFaces => _selectedFaces;

    private HashSet<Edge> _selectedEdges = [];
    public IReadOnlySet<Edge> SelectedEdges => _selectedEdges;


    /// <summary>
    /// First parameter is the index, second parameter is true if selected, false if deselected
    /// </summary>
    public event Action<bool, UpdateType>? OnSelectionChanged;

    public event Action<UpdateType>? OnSelectionMassUpdate;

    public void SelectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        if(index >= Model.Indicies.Count()) throw new ArgumentOutOfRangeException($"The index {index} exceeds the possible selection range!");

        _selectedIndicies.Add(index);
        OnSelectionChanged?.Invoke(true, updateInfo);
    }

    public void DeselectAll(UpdateType updateInfo = UpdateType.Selection)
    {
        Console.WriteLine("selection cleared!");
        _selectedFaces.Clear();
        _selectedEdges.Clear();
        _selectedIndicies.Clear();
        OnSelectionMassUpdate?.Invoke(updateInfo);
    }

    public void BroadcastMassUpdate(UpdateType updateInfo) => OnSelectionMassUpdate?.Invoke(updateInfo);

    public void DeselectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        _selectedIndicies.Remove(index);
        OnSelectionChanged?.Invoke(false, updateInfo);
    }

    public override void Dispose() { }

    public override void OnModelUpdate(Model model, UpdateType info)
    {

    }

    public override void OnAddedToModel(Model model) { }

    public bool IsVertexSelected(uint i) => _selectedIndicies.Contains(i);

    public static bool BindComponent(Model model)
    {
        if (!model.HasComponent(typeof(SelectionComponent)))
            model.AddComponent<SelectionComponent>(new SelectionComponent());
        return true;
    }

    public IEnumerable<uint> SelectionIndicies()
    {
        foreach(uint item in _selectedIndicies) yield return item;
    }

    public Vector3 GetWorldCenter()
    {
        return Model.TransformPointByModelMatrix(GetCenter());
    }

    public Vector3 GetCenter()
    {
        var vertSelection = GetSelection<uint>().ToArray();

        if (vertSelection.Count() == 0)
            return Vector3.Zero;

        Vector3 sum = Vector3.Zero;

        foreach (uint ind in vertSelection)
        {
            Vertex v = Model.GetVertex(ind);
            sum += v.Position;
        }

        return sum / vertSelection.Count();
    }

    public void SelectEdge(Edge edge, UpdateType updateType = UpdateType.Selection)
    {
        _selectedEdges.Add(edge);
        OnSelectionChanged?.Invoke(true, updateType);
    }
    public void DeselectFace(Face face, UpdateType updateType = UpdateType.Selection)
    {
        _selectedFaces.Remove(face);
        OnSelectionChanged?.Invoke(false, updateType);
    }
    public void DeselectEdge(Edge edge, UpdateType updateType = UpdateType.Selection)
    {
        _selectedEdges.Remove(edge);
        OnSelectionChanged?.Invoke(false, updateType);
    }

    public void SelectFace(Face face, UpdateType updateType = UpdateType.Selection)
    {
        _selectedFaces.Add(face);
        OnSelectionChanged?.Invoke(true, updateType);
    }
    /// <summary>
    /// Returns a selection of your desired type. Will translate between selection modes automatically.
    /// </summary>
    /// <typeparam name="T"> This will be either a Face, Vertex or Edge.</typeparam>
    /// <remarks> Face -> Edge-> Vertex!  If you desire faces and only edges are selected. Then you get nothing!</remarks>
    /// <returns>A list of <typeparamref name="T"/> which is <see cref="Face"/>, <see cref="uint"/>, or <see cref="Edge"/> depending on what your generic desires.</returns>
    public IEnumerable<T> GetSelection<T>()
    {
        if (typeof(T) == typeof(Face))
        {
            foreach (Face selectedObject in _selectedFaces)
            {
                yield return (T)(object)selectedObject;
            }
        }
        else if (typeof(T) == typeof(Edge))
        {
            //Search for edges
            HashSet<Edge> edges = [];
            foreach (Edge edge in _selectedEdges) edges.Add(edge);

            foreach (Face face in _selectedFaces)
            {
                foreach (Edge faceEdge in face.Edges)
                {
                    edges.Add(faceEdge);
                }
            }
            //yield those found from search
            foreach (Edge edge in edges)
                yield return (T)(object)edge;
        }
        else if (typeof(T) == typeof(uint))//index
        {
            //Search for verts
            HashSet<uint> verts = [];

            foreach (uint index in _selectedIndicies) verts.Add(index);

            foreach(Edge edge in _selectedEdges)
            {
                verts.Add(edge.Vertex1);
                verts.Add(edge.Vertex2);
            }

            foreach(Face face in _selectedFaces)
            {
                foreach(uint index in face.Indicies) verts.Add(index);
            }
            
            foreach (uint index in verts)
            {
                yield return (T)(object)index;
            }
        }
    }

    public bool IsEdgeSelected(Edge edge) => _selectedEdges.Contains(edge);

    public bool IsFaceSelected(Face face) => _selectedFaces.Contains(face);

    public IEnumerable<object> SelectionBucket()
    {
        foreach (Face face in _selectedFaces) yield return face;

        foreach (Edge edge in _selectedEdges) yield return edge;

        foreach(uint index in  _selectedIndicies) yield return index;
    }
}
