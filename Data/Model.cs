using OpenTK.Mathematics;

namespace Models;

public class Model
{
    public string ObjectName = "Model";
    public List<Vertex> Verticies { get; private set; } = [];
    protected List<Face> _Faces = [];
    protected HashSet<Edge> _Edges = [];
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

    public Vertex GetVertex(uint index)
    {
        if(index < 0 || index > Verticies.Count -1)
        {
            throw new ArgumentOutOfRangeException($"{nameof(index)} is out of range");
        }
        return Verticies[(int)index];
    }

    public void AddVertex(Vertex vertex)
    {
        Verticies.Add(vertex);
        UpdateAllComponents(UpdateType.Membership, vertex);
    }

    public void AddEdge(Edge edge, UpdateType info = UpdateType.None)
    {
        if (_Edges.TryGetValue(edge, out Edge? hashEdge) && hashEdge != null)
        {
           hashEdge.Faces.AddRange(edge.Faces);
        }
        else
            _Edges.Add(edge);

        UpdateAllComponents(UpdateType.Membership | info, edge);
    }

    public void AddFace(params uint[] indicies) => AddFace(new List<uint>(indicies), UpdateType.Membership);
    public void AddFace(List<uint> indicies, UpdateType info = UpdateType.Membership) => AddFace(new Face(indicies));
    public void AddFace(Face face, UpdateType info = UpdateType.Membership)
    {
        foreach(uint i in face.Indicies)
        {
            if( i < 0 || i > Verticies.Count -1)
            {
                throw new ArgumentException($"{nameof(face)} has an index out of range");
            }
        }

        face.ParentModel = this;
        _Faces.Add(face);

        for(int i = 0; i < face.Indicies.Count - 1; i++) {
            uint start = (uint)face.Indicies[i];
            uint end = (uint)face.Indicies[i + 1];
            AddEdge(new Edge(start, end, face), UpdateType.Ignore);
        }
        AddEdge(new Edge(face.Indicies[0], face.Indicies[^1], face), UpdateType.Ignore);
        UpdateAllComponents(info, face);
    }

    public void RemoveVertex(int index, UpdateType info = UpdateType.Membership)
    {

        //Manage faces
        int editCount = _Faces.RemoveAll(x => x.Contains((uint)index));
        _Faces.ForEach(face => face.DecrementForIndex(index));

        //Manage edges
        editCount += _Edges.RemoveWhere(x => x.Contains(index));

        //Remap old edges by replacing them with new ones.
        foreach (Edge e in _Edges.Where(x => x.RequiresDecrement(index)).Select(x => x).ToArray())
        {
            _Edges.Remove(e);
            e.DecrementForIndex(index);
            _Edges.Add(e);
        }

        Verticies.RemoveAt(index);
        UpdateAllComponents(info, index);
    }

    public void RemoveFace(Face face, UpdateType info = UpdateType.Membership)
    {
        _Faces.Remove(face);
        UpdateAllComponents(info, face);
    }

    public void RemoveEdge(Edge edge, UpdateType info = UpdateType.Membership)
    {
        foreach(Face f in edge.Faces)
        {
            RemoveFace(f, info);
        }

        _Edges.Remove(edge);
        UpdateAllComponents(info, edge);
    }

    private void GenerateIndicies()
    {
        TriangleToFaceMapping.Clear();
        List<uint> indicies = [];
        foreach (Face face in _Faces)
        {
            face.Triangulate(ref indicies);
        }
        Indicies = indicies.ToArray();
    }

    public uint[] GetEdgeIndicies()
    {
        uint[] indicies = new uint[_Edges.Count * 2];
        int index = 0;
        foreach (Edge edge in _Edges)
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

    public bool HasComponent(Type type) => _Components.ContainsKey(type);

    public void UpdateAllComponents(UpdateType info, object? variable)
    {
        foreach (var component in _Components.Values) component.OnModelUpdate(this, info, variable);
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
        verts = Verticies.BackingField();
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

    public IEnumerable<Face> Faces => _Faces;

    public IEnumerable<Edge> Edges => _Edges;
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

        Verticies.Clear();
        _Faces.Clear();
        _Edges.Clear();

        Verticies = [.. modelState.Verticies];
        foreach (Face face in modelState.Faces)
            AddFace((Face)face.Clone(), UpdateType.Ignore);

        foreach (Edge edge in modelState.Edges)
            AddEdge((Edge)edge.Clone(), UpdateType.Ignore);

        GenerateIndicies();
        UpdateAllComponents(UpdateType.Membership, null);
    }
}
