using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Core;

public class Model
{
    public string ObjectName { get; set; } = "Model";
    //Verts
    private List<Vertex> _verticies = [];
    public IReadOnlyList<Vertex> Verticies => _verticies;

    private readonly List<HashSet<uint>> _vertexEdgeMap = [];
    public IReadOnlyList<HashSet<uint>> VertexEdgeMap => _vertexEdgeMap;

    private readonly List<Face> _faces = [];
    public IReadOnlyList<Face> Faces => _faces;
    private readonly HashSet<Edge> _edges = [];
    public IReadOnlySet<Edge> Edges => _edges;
    protected bool IsDisposed { get; set; }
    public uint[] Indices = [];
    public bool Hidden { get; set; }
    public HierarchyType HierarchyType { get; set; } = HierarchyType.All;

    private readonly Dictionary<Type, ModelComponent> _components = [];

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    //Store starting index
    public readonly Dictionary<(uint,uint,uint), Face> TriangleToFaceMapping = [];

    /// <summary>
    /// Model clone constructor.
    /// </summary>
    public Model(Model clone)
    {
        EmplaceData(clone);
        Position = clone.Position;
        Rotation = clone.Rotation;
        Scale = clone.Scale;
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
            throw new ArgumentOutOfRangeException(nameof(index));
        return _verticies[(int)index];
    }

    public uint AddVertex(Vertex vertex, UpdateType updateType = UpdateType.Membership)
    {
        uint index = (uint)_verticies.Count;
        _verticies.Add(vertex);
        _vertexEdgeMap.Add([]);
        UpdateAllComponents(updateType);
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool VertexExists(uint index) => index < Verticies.Count;

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
            _vertexEdgeMap[(int)edge.Vertex1].Add(edge.Vertex2);
            _vertexEdgeMap[(int)edge.Vertex2].Add(edge.Vertex1);
            retEdge = edge;
        }

        UpdateAllComponents(updateType);
        return retEdge;


    }


    public void AddFace(params uint[] indices) => AddFaceWithMembership(UpdateType.Membership, indices);
    public void AddFaceWithMembership(UpdateType updateType, params uint[] indices) => AddFace([.. indices], updateType);
    public void AddFace(List<uint> indices, UpdateType info = UpdateType.Membership) => AddFace(new Face(indices), info);
    public void AddFace(Face face, UpdateType info = UpdateType.Membership)
    {
        foreach(uint i in face.Indices)
        {
            if(!VertexExists(i))
            {
                throw new ArgumentException($"{nameof(face)} has an index out of range");
            }
        }

        face.ParentModel = this;
        _faces.Add(face);

        for(int i = 0; i < face.Indices.Count - 1; i++) {
            uint start = (uint)face.Indices[i];
            uint end = (uint)face.Indices[i + 1];
            Edge edge = AddEdge(new Edge(start, end, face), UpdateType.Ignore);

            face.Edges.Add(edge);
        }
        Edge edgeOverlap = AddEdge(new Edge(face.Indices[0], face.Indices[^1], face), UpdateType.Ignore);
        face.Edges.Add(edgeOverlap);
        UpdateAllComponents(info);
    }

    public void RemoveVertex(int index, UpdateType info = UpdateType.Membership)
    {
        if (index >= _verticies.Count)
            throw new InvalidOperationException("Trying to delete a index that DNE");

        //Clear out edgemap and our neighbors
        uint uIndex = (uint)index;
        //Remove US from our NEIGHBORS
        foreach (uint neighbor in _vertexEdgeMap[index])
        {
            if (neighbor < _vertexEdgeMap.Count)
                _vertexEdgeMap[(int)neighbor].Remove(uIndex);
        }
        //Remove US
        _vertexEdgeMap.RemoveAt(index);

        //Decrement edgemap set
        foreach (HashSet<uint> edgeIndices in _vertexEdgeMap)
        {
            foreach (uint neighbor in edgeIndices.ToArray())
            {
                if(neighbor > index)
                {
                    //Decrement!
                    edgeIndices.Remove(neighbor);
                    edgeIndices.Add(neighbor - 1);
                }
            }
        }

        //Manage faces
        _faces.RemoveAll(x => x.Contains(uIndex));
        _faces.ForEach(face => face.DecrementForIndex(index));

        //Manage edges
        _edges.RemoveWhere(x => x.Contains(index));


        HashSet<Edge> overflowEdges = [];
        //Remap old edges by replacing them with new ones.
        foreach (Edge edge in _edges.Where(x => x.RequiresDecrement(index)).Select(x => x).ToArray())
        {
            _edges.Remove(edge);
            edge.DecrementForIndex(index);

            if(!_edges.Add(edge))
                overflowEdges.Add(edge);
        }
        //Add edges we couldn't due to hash issues.
        foreach (Edge edge in overflowEdges) _edges.Add(edge);


        _verticies.RemoveAt(index);
        UpdateAllComponents(info);
    }

    public void RemoveFace(Face face, UpdateType info = UpdateType.Membership, Edge? ignoreEdge = null)
    {
        //Tell our children we don't exist. Ready for GC.
        foreach (Edge edge in face.Edges)
        {
            if(edge != ignoreEdge)//Ignore edge if required
                edge.Faces.Remove(face);
        }

        _faces.Remove(face);
        
        UpdateAllComponents(info);
    }

    public void RemoveEdge(Edge edge, UpdateType info = UpdateType.Membership)
    {
        foreach(Face f in edge.Faces)
        {
            RemoveFace(f, info, edge);
        }
        _vertexEdgeMap[(int)edge.Vertex1].Remove(edge.Vertex2);
        _vertexEdgeMap[(int)edge.Vertex2].Remove(edge.Vertex1);

        _edges.Remove(edge);
        UpdateAllComponents(info);
    }

    private void GenerateIndices()
    {
        TriangleToFaceMapping.Clear();
        List<uint> indices = [];
        foreach (Face face in _faces)
        {
            face.Triangulate(ref indices);
        }
        Indices = [.. indices];
    }

    public uint[] GetEdgeIndices()
    {
        uint[] indices = new uint[_edges.Count * 2];
        int index = 0;
        foreach (Edge edge in _edges)
        {
            indices[index++] = edge.Vertex1;
            indices[index++] = edge.Vertex2;
        }
        return indices;
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
    /// <summary>
    /// The models Scale,Rotation,Position matrix
    /// </summary>
    public Matrix4 GetModelMatrix()
    {
        return GetScaleMatrix()
            * GetRotationMatrix()
            * GetTranslationMatrix();
    }

    public Vector3 TransformPointByModelMatrix(Vector3 point)
    {
        Vector4 point4d = new(point, 1.0f);
        point4d = GetModelMatrix() * point4d;
        return point4d.Xyz;
    }

    public ModelComponent AddComponent<T>(ModelComponent component)
    {
        _components[typeof(T)] = component;
        component.Model = this;
        component.OnAddedToModel(this);
        return component;
    }

    public bool RemoveComponent(Type component) => _components.Remove(component);

    public T? GetComponent<T>() where T : ModelComponent
    {
        Type type = typeof(T);

        if (_components.TryGetValue(type, out var component))
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
        if (_components.TryGetValue(typeof(T), out var obj))
        {
            component = obj;
            return true;
        }
        component = default;
        return false;
    }

    public IEnumerable<T> GetAllOfType<T>()
    {
        foreach(KeyValuePair<Type, ModelComponent> component in _components)
        {
            if(component.Value is T typedValue)
            {
                yield return typedValue;
            }
        }
    }

    public bool HasComponent(Type type) => _components.ContainsKey(type);

    public void UpdateAllComponents(UpdateType info)
    {
        foreach (var component in _components.Values) component.OnModelUpdate(this, info);
    }

    public IEnumerable<(uint v1,uint v2,uint v3)> AllTrianglesAsIndices()
    {
        for(int i = 0; i < Indices.Length; i+=3)
        {
            yield return (Indices[i], Indices[i + 1], Indices[i + 2]);
        }
    }

    public void GenerateTriangulatedModel(ref Vertex[] verts, ref List<uint> indices)
    {
        verts = _verticies.BackingField();
        GenerateIndices();
        indices = [.. Indices];
    }

    ~Model() => Dispose();

    public void Dispose()
    {
        IsDisposed = true;
        foreach (var component in _components.Values)
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

    public void EmplaceData(Model modelState)
    {
        _verticies.Clear();
        _faces.Clear();
        _edges.Clear();

        _verticies = [.. modelState._verticies];
        _vertexEdgeMap.Clear();
        foreach (HashSet<uint> edgeSet in modelState.VertexEdgeMap)
        {
            _vertexEdgeMap.Add([.. edgeSet]);
        }

        foreach (Face face in modelState.Faces)
            AddFace((Face)face.Clone(), UpdateType.Ignore);

        foreach (Edge edge in modelState.Edges)
            AddEdge((Edge)edge.Clone(), UpdateType.Ignore);

        GenerateIndices();
        UpdateAllComponents(UpdateType.Membership);
    }
    public Vector3 ComputeCenter()
    {
        Vector3 center = new(0);
        foreach(Vertex vertex in Verticies)
        {
            center += vertex.Position;
        }
        return center / Verticies.Count;
    }
    public Vector3 ComputeCenterWorldSpace()
    {
        var translated = new Vector4(ComputeCenter(), 1.0f);
        translated *= GetModelMatrix();
        return translated.Xyz;
    }
    /// <summary>
    /// Given a Face or Edge, the proper object is returned.
    /// Allows a dummy Edge() or dummy Face() to yield a valid Edge/Face.
    /// 
    /// This exists because Face and Edge have neighbor metadata that dummy instances fail to capture.
    /// </summary>
    /// <remarks>This is an O(n) operation! Where N is #Faces or #Edges</remarks>
    public bool Lookup<T>(T data, out T? found) where T : class
    {
        found = null;
        if (data is Face face)
        {
            foreach (Face faceLookup in Faces)
            {
                if (!faceLookup.Equals(faceLookup)) continue;

                found = (T?)(object)faceLookup;
                return true;
            }
        }
        else if (data is Edge edge)
        {
            foreach(Edge edgeLookup in Edges)
            {
                if (!edgeLookup.Equals(edge)) continue;

                found = (T?)(object)edgeLookup;
                return true;
            }
        }
        return false;
    }
}
