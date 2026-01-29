using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Models;

public class Model : IDisposable
{
    public string ObjectName = "Model";
    public List<Vertex> Verticies { get; private set; } = [];
    private List<Face> _Faces = [];
    private HashSet<Edge> _Edges = [];
    protected bool IsDisposed = false;

    public uint[] Indicies = [];
    public bool Hidden = false;

    private Dictionary<Type, ModelComponent> _Components = [];

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    public Dictionary<(uint,uint,uint), Face> TriangleToFaceMapping = [];

    public Vertex GetVertex(int index) => Verticies[index];

    public void AddVertex(Vertex vertex)
    {
        Verticies.Add(vertex);
        UpdateAllComponents(UpdateType.Vertex | UpdateType.Membership, vertex);
    }

    public void AddEdge(Edge edge, UpdateType info = UpdateType.None)
    {
        _Edges.Add(edge);
        UpdateAllComponents(UpdateType.Edge | UpdateType.Membership | info, edge);
    }

    public void AddFace(params uint[] indicies) => AddFace(new List<uint>(indicies));
    public void AddFace(List<uint> indicies) => AddFace(new Face(indicies));
    public void AddFace(Face face)
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
            AddEdge(new Edge(start, end), UpdateType.Ignore);
        }
        AddEdge(new Edge(face.Indicies[0], face.Indicies[^1]), UpdateType.Ignore);
        UpdateAllComponents(UpdateType.Face | UpdateType.Membership, face);
    }

    public void RemoveVertex(int index)
    {
        Verticies.RemoveAt(index);

        //Manage faces
        int editCount = _Faces.RemoveAll(x => x.Contains((uint)index));
        _Faces.ForEach(face => face.DecrementForIndex(index));

        //Manage edges
        editCount += _Edges.RemoveWhere(x => x.Contains(index));
        foreach(Edge e  in _Edges)
        {
            e.DecrementForIndex(index);
        }
        UpdateAllComponents(UpdateType.Vertex | UpdateType.Membership, index);
    }

    public void RemoveFace(Face face)
    {
        _Faces.Remove(face);
        UpdateAllComponents(UpdateType.Face | UpdateType.Membership, face);
    }

    public void RemoveEdge(Edge edge)
    {
        _Edges.Remove(edge);
        UpdateAllComponents(UpdateType.Edge | UpdateType.Membership, edge);
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

    public ModelComponent AddComponent<T>(ModelComponent component)
    {
        if(component is null) throw new ArgumentNullException($"Invalid component: {nameof(T)} | {nameof(component)}");
        _Components[typeof(T)] = component;
        component.model = this;
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

    void UpdateAllComponents(UpdateType info, object variable)
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
        verts = Verticies.ToArray();
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
}
