using OpenTK.Mathematics;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Models;

public class Model
{
    public string ObjectName = "Model";
    //Verts
    private List<Vertex> _verticies = [];
    public IReadOnlyList<Vertex> Verticies => _verticies;

    internal Dictionary<uint, HashSet<uint>> _vertexEdgeMap = [];
    public IReadOnlyDictionary<uint, HashSet<uint>> VertexEdgeMap => _vertexEdgeMap;

    protected List<Face> _faces = [];
    public IReadOnlyList<Face> Faces => _faces;
    protected HashSet<Edge> _edges = [];
    public IReadOnlySet<Edge> Edges => _edges;
    protected bool IsDisposed = false;
    public uint[] Indicies = [];
    public bool Hidden = false;
    public HierarchyType hierarchyType = HierarchyType.All;

    private Dictionary<Type, ModelComponent> _Components = [];

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    //Store starting index
    public Dictionary<(uint,uint,uint), Face> TriangleToFaceMapping = [];

    /// <summary>
    /// Model clone constructor.
    /// </summary>
    public Model(Model clone)
    {
        EmplaceData(clone);
    }

    public Model() { }

    public bool TryGetVertex(uint index, out Vertex? vertex)
    {
        vertex = null;
        try
        {
            vertex = GetVertex(index);
            return true;
        }
        catch(ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public Vertex[] GetVertexBackingField()
    {
        return _verticies.BackingField<Vertex>();
    }

    public Vertex GetVertex(uint index)
    {
        if(index < 0 || index > _verticies.Count -1)
        {
            throw new ArgumentOutOfRangeException($"{nameof(index)} is out of range");
        }
        return _verticies[(int)index];
    }

    public uint AddVertex(Vertex vertex, UpdateType updateType = UpdateType.Membership)
    {
        uint index = (uint)_verticies.Count();
        _verticies.Add(vertex);
        _vertexEdgeMap.Add(index, new());
        UpdateAllComponents(updateType);
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool VertexExists(uint index) => index < Verticies.Count();

    public Edge AddEdge(Edge edge, UpdateType updateType = UpdateType.Membership)
    {
        if (!VertexExists(edge.Vertex1) || !VertexExists(edge.Vertex2))
            throw new InvalidOperationException("Edge added with verts out of range.");

        Edge retEdge;
        if (_edges.TryGetValue(edge, out Edge? hashEdge) && hashEdge != null)
        {
            hashEdge.Faces.AddRange(edge.Faces);
            retEdge = hashEdge;
        }
        else
        {
            _edges.Add(edge);
            _vertexEdgeMap[edge.Vertex1].Add(edge.Vertex2);
            _vertexEdgeMap[edge.Vertex2].Add(edge.Vertex1);
            retEdge = edge;
        }

        UpdateAllComponents(updateType);
        return retEdge;


    }


    public void AddFace(params uint[] indicies) => AddFaceUpdate(UpdateType.Membership, indicies);
    public void AddFaceUpdate(UpdateType updateType, params uint[] indicies) => AddFace([.. indicies], updateType);
    public void AddFace(List<uint> indicies, UpdateType info = UpdateType.Membership) => AddFace(new Face(indicies), info);
    public void AddFace(Face face, UpdateType info = UpdateType.Membership)
    {
        foreach(uint i in face.Indicies)
        {
            if(!VertexExists(i))
            {
                throw new ArgumentException($"{nameof(face)} has an index out of range");
            }
        }

        face.ParentModel = this;
        _faces.Add(face);

        for(int i = 0; i < face.Indicies.Count - 1; i++) {
            uint start = (uint)face.Indicies[i];
            uint end = (uint)face.Indicies[i + 1];
            Edge edge = AddEdge(new Edge(start, end, face), UpdateType.Ignore);

            face.Edges.Add(edge);
        }
        Edge edgeOverlap = AddEdge(new Edge(face.Indicies[0], face.Indicies[^1], face), UpdateType.Ignore);
        face.Edges.Add(edgeOverlap);
        UpdateAllComponents(info);
    }

    public void RemoveVertex(int index, UpdateType info = UpdateType.Membership)
    {
        if (index >= _verticies.Count)
            throw new InvalidOperationException("Trying to delete a index that DNE");

        //Clear out edgemap and our neighbors
        uint uIndex = (uint)index;
        if (_vertexEdgeMap.ContainsKey(uIndex))
        {
            foreach (uint neighbor in _vertexEdgeMap[uIndex])
            {
                if(_vertexEdgeMap.TryGetValue(neighbor, out var neighborList))
                {
                    neighborList.Remove(uIndex);//Clear index out of it's neighbors.
                }
            }
            _vertexEdgeMap.Remove(uIndex);//Clear index
        }

        //Manage faces
        int editCount = _faces.RemoveAll(x => x.Contains(uIndex));
        _faces.ForEach(face => face.DecrementForIndex(index));

        //Manage edges
        editCount += _edges.RemoveWhere(x => x.Contains(index));

        //Remap old edges by replacing them with new ones.
        foreach (Edge e in _edges.Where(x => x.RequiresDecrement(index)).Select(x => x).ToArray())
        {
            _edges.Remove(e);
            e.DecrementForIndex(index);
            _edges.Add(e);
        }

        _verticies.RemoveAt(index);
        UpdateAllComponents(info);
    }

    public void RemoveFace(Face face, UpdateType info = UpdateType.Membership)
    {
        _faces.Remove(face);
        UpdateAllComponents(info);
    }

    public void RemoveEdge(Edge edge, UpdateType info = UpdateType.Membership)
    {
        foreach(Face f in edge.Faces)
        {
            RemoveFace(f, info);
        }
        _vertexEdgeMap[edge.Vertex1].Remove(edge.Vertex2);
        _vertexEdgeMap[edge.Vertex2].Remove(edge.Vertex1);

        _edges.Remove(edge);
        UpdateAllComponents(info);
    }

    private void GenerateIndicies()
    {
        TriangleToFaceMapping.Clear();
        List<uint> indicies = [];
        foreach (Face face in _faces)
        {
            face.Triangulate(ref indicies);
        }
        Indicies = indicies.ToArray();
    }

    public uint[] GetEdgeIndicies()
    {
        uint[] indicies = new uint[_edges.Count * 2];
        int index = 0;
        foreach (Edge edge in _edges)
        {
            indicies[index++] = edge.Vertex1;
            indicies[index++] = edge.Vertex2;
        }
        return indicies;
    }

    public Matrix4 GetRotationMatrix()
    {
        Matrix4 rotX = Matrix4.CreateRotationX(Rotation.X);
        Matrix4 rotY = Matrix4.CreateRotationY(Rotation.Y);
        Matrix4 rotZ = Matrix4.CreateRotationZ(Rotation.Z);
        return rotZ * rotY * rotX; // ZYX order of rotation...
    }

    public Matrix4 GetScaleMatrix() => Matrix4.CreateScale(Scale);

    public Matrix4 GetTranslationMatrix() => Matrix4.CreateTranslation(Position);

    public Matrix4 GetModelMatrix()
    {
        return GetScaleMatrix()
            * GetRotationMatrix()
            * GetTranslationMatrix();
    }

    public Vector3 TransformPointByModelMatrix(Vector3 point)
    {
        Vector4 point4d = new Vector4(point, 1.0f);
        point4d = GetModelMatrix() * point4d;
        return point4d.Xyz;
    }

    public ModelComponent AddComponent<T>(ModelComponent component)
    {
        if(component is null) throw new ArgumentNullException($"Invalid component: {nameof(T)} | {nameof(component)}");
        _Components[typeof(T)] = component;
        component.Model = this;
        component.OnAddedToModel(this);
        return component;
    }

    public bool RemoveComponent(Type component) => _Components.Remove(component);

    public T? GetComponent<T>() where T : ModelComponent
    {
        Type type = typeof(T);

        if (_Components.TryGetValue(type, out var component))
        {
            return (T)component;
        }

        return null;
    }

    public bool TryGetComponent<T>(out T? component) where T : ModelComponent
    {
        component = GetComponent<T>();
        return component != null;
    }

    public bool TryGetComponent<T>(out ModelComponent? component)
    {
        if (_Components.TryGetValue(typeof(T), out var obj))
        {
            component = obj;
            return true;
        }
        component = default;
        return false;
    }

    public IEnumerable<T> GetAllOfType<T>()
    {
        foreach(KeyValuePair<Type, ModelComponent> component in _Components)
        {
            if(component.Value is T typedValue)
            {
                yield return typedValue;
            }
        }
    }

    public bool HasComponent(Type type) => _Components.ContainsKey(type);

    public void UpdateAllComponents(UpdateType info)
    {
        foreach (var component in _Components.Values) component.OnModelUpdate(this, info);
    }

    public IEnumerable<(uint v1,uint v2,uint v3)> AllTrianglesAsIndicies()
    {
        for(int i = 0; i < Indicies.Length; i+=3)
        {
            yield return (Indicies[i], Indicies[i + 1], Indicies[i + 2]);
        }
    }

    public void GenerateTriangulatedModel(ref Vertex[] verts, ref List<uint> indicies)
    {
        verts = _verticies.BackingField();
        GenerateIndicies();
        indicies = new (Indicies);
    }

    ~Model() => Dispose();

    public void Dispose()
    {
        IsDisposed = true;
        foreach (var component in _Components.Values)
        {
            component.Dispose();
        }
    }
    /// <summary>
    /// Clones the model data such as Verticies, Faces, Edges and name.
    /// Does not clone the models components.
    /// </summary>
    /// <returns></returns>
    public Model Clone()
    { 
        return new Model(this);
    }

    public void EmplaceData(Model? modelState)
    {
        if(modelState == null)
            throw new ArgumentNullException(nameof(modelState));

        _verticies.Clear();
        _faces.Clear();
        _edges.Clear();

        _verticies = [.. modelState._verticies];
        _vertexEdgeMap.Clear();
        foreach (var pair in modelState.VertexEdgeMap)
        {
            _vertexEdgeMap.Add(pair.Key, pair.Value);
        }

        foreach (Face face in modelState.Faces)
            AddFace((Face)face.Clone(), UpdateType.Ignore);

        foreach (Edge edge in modelState.Edges)
            AddEdge((Edge)edge.Clone(), UpdateType.Ignore);

        GenerateIndicies();
        UpdateAllComponents(UpdateType.Membership);
    }
}
