using Core;
using OpenTK.Mathematics;
using System.Linq;
using System.Collections.Generic;

namespace UI.ViewModels;

public class SelectionComponent : ModelComponent
{
    private SortedSet<uint> _selectedIndices = [];
    public IReadOnlySet<uint> SelectedIndices => _selectedIndices; 

    private HashSet<Face> _selectedFaces = [];
    public IReadOnlySet<Face> SelectedFaces => _selectedFaces;

    private HashSet<Edge> _selectedEdges = [];
    public IReadOnlySet<Edge> SelectedEdges => _selectedEdges;

    public object? LastSelection { get; private set; }

    /// <summary>
    /// First parameter is the index, second parameter is true if selected, false if deselected
    /// </summary>
    public event Action<bool, UpdateType>? OnSelectionChanged;

    public event Action<UpdateType>? OnSelectionMassUpdate;

    public void SelectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        if(index >= Model.Indices.Length) throw new ArgumentOutOfRangeException($"The index {index} exceeds the possible selection range!");

        _selectedIndices.Add(index);
        LastSelection = index;
        OnSelectionChanged?.Invoke(true, updateInfo);
    }

    public void DeselectAll(UpdateType updateInfo = UpdateType.Selection)
    {
        foreach (Face face in _selectedFaces)
        {
            SelectionMeshInstance.Instance.DeselectFace(face);
        }

        _selectedFaces.Clear();
        _selectedEdges.Clear();
        _selectedIndices.Clear();
        LastSelection = null;
        OnSelectionMassUpdate?.Invoke(updateInfo);
    }

    public void BroadcastMassUpdate(UpdateType updateInfo) => OnSelectionMassUpdate?.Invoke(updateInfo);

    public void DeselectIndex(uint index, UpdateType updateInfo = UpdateType.Selection)
    {
        _selectedIndices.Remove(index);
        OnSelectionChanged?.Invoke(false, updateInfo);
    }

    public override void Dispose() 
    {
        GC.SuppressFinalize(this);
    }

    public override void OnModelUpdate(Model model, UpdateType info)
    {

    }

    public override void OnAddedToModel(Model model) { }

    public bool IsVertexSelected(uint i) => _selectedIndices.Contains(i);

    public static bool BindComponent(Model model)
    {
        if (!model.HasComponent(typeof(SelectionComponent)))
            model.AddComponent<SelectionComponent>(new SelectionComponent());
        return true;
    }
    /// <summary>
    /// Yields all indices ordered from largest to smallest
    /// This functionality is to help with deletion operations, which might re-shuffle your selected set.
    /// Keep this in mind, as it might become part of your nightmares.
    /// </summary>
    public IEnumerable<uint> SelectionIndices()
    {
        foreach(uint item in _selectedIndices.Reverse()) yield return item;
    }

    public Vector3 GetWorldCenter()
    {
        return Model.TransformPointByModelMatrix(GetCenter());
    }

    public Vector3 GetCenter()
    {
        var vertSelection = GetSelection<uint>().ToArray();

        if (vertSelection.Length == 0)
            return Vector3.Zero;

        Vector3 sum = Vector3.Zero;

        foreach (uint ind in vertSelection)
        {
            Vertex v = Model.GetVertex(ind);
            sum += v.Position;
        }

        return sum / vertSelection.Length;
    }

    public void SelectEdge(Edge edge, UpdateType updateType = UpdateType.Selection)
    {
        _selectedEdges.Add(edge);
        LastSelection = edge;
        OnSelectionChanged?.Invoke(true, updateType);
    }
    public void DeselectFace(Face face, UpdateType updateType = UpdateType.Selection)
    {
        _selectedFaces.Remove(face);
        OnSelectionChanged?.Invoke(false, updateType);
        SelectionMeshInstance.Instance.DeselectFace(face);
    }
    public void DeselectEdge(Edge edge, UpdateType updateType = UpdateType.Selection)
    {
        _selectedEdges.Remove(edge);
        OnSelectionChanged?.Invoke(false, updateType);
    }

    public void SelectFace(Face face, UpdateType updateType = UpdateType.Selection)
    {
        _selectedFaces.Add(face);
        LastSelection = face;
        OnSelectionChanged?.Invoke(true, updateType);
        SelectionMeshInstance.Instance.SelectFace(face);
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

            foreach (uint index in _selectedIndices.Reverse()) verts.Add(index);

            foreach(Edge edge in _selectedEdges)
            {
                verts.Add(edge.Vertex1);
                verts.Add(edge.Vertex2);
            }

            foreach(Face face in _selectedFaces)
            {
                foreach(uint index in face.Indices) verts.Add(index);
            }
            
            foreach (uint index in verts)
            {
                yield return (T)(object)index;
            }
        }
    }

    public bool IsEdgeSelected(Edge edge) => _selectedEdges.Contains(edge);

    public bool IsFaceSelected(Face face) => _selectedFaces.Contains(face);

    ///<summary>
    /// Only yields Face, Edge and uints
    /// Use this function if you are expecting to deal with all of these at once.
    ///</summary>
    ///<returns>Faces, then Edges, then sorted indices (descending)</returns>
    /// <remarks> uints are returned sorted from largest to smallest. (Descending order)</remarks>
    public IEnumerable<object> SelectionBucket()
    {
        foreach (Face face in _selectedFaces) yield return face;

        foreach (Edge edge in _selectedEdges) yield return edge;

        ///IMPORTANT NOTE: We feed the indices through in reverse!
        ///This is to simplify deletion, deleting indices from largest to smallest keeps the
        ///indices in tact! (Deletion causes mass re-shuffling, this mitigates it's affect on most logic)
        foreach(uint index in  _selectedIndices.Reverse()) yield return index;
    }
}
