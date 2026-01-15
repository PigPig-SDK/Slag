using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using OpenTK.Mathematics;

namespace Models;

public class Model
{
    public string ObjectName = "Object";
    public List<Vertex> Verticies { get; private set; } = [];
    private List<Face> _Faces = [];
    private HashSet<Edge> _Edges = [];
    public uint[] Indicies = [];

    private Dictionary<Type, ModelComponent> _Components = [];

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;

    public Dictionary<(uint,uint,uint), Face> TriangleToFaceMapping = [];


    public Vertex GetVertex(int index) => Verticies[index];

    public void AddVertex(Vertex vertex)
    {
        Verticies.Add(vertex);
        UpdateAllComponents(ModelUpdateType.Vertex | ModelUpdateType.Membership, vertex);
    }

    public void AddEdge(Edge edge, ModelUpdateType info = ModelUpdateType.None)
    {
        _Edges.Add(edge);
        UpdateAllComponents(ModelUpdateType.Edge | ModelUpdateType.Membership | info, edge);
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
            AddEdge(new Edge(start, end), ModelUpdateType.MassOperation);
        }
        UpdateAllComponents(ModelUpdateType.Face | ModelUpdateType.Membership, face);
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
        UpdateAllComponents(ModelUpdateType.Vertex | ModelUpdateType.Membership, index);
    }

    public void RemoveFace(Face face)
    {
        _Faces.Remove(face);
        UpdateAllComponents(ModelUpdateType.Face | ModelUpdateType.Membership, face);
    }

    public void RemoveEdge(Edge edge)
    {
        _Edges.Remove(edge);
        UpdateAllComponents(ModelUpdateType.Edge | ModelUpdateType.Membership, edge);
    }

    public void GenerateIndicies()
    {
        TriangleToFaceMapping.Clear();
        List<uint> indicies = [];
        foreach (Face face in _Faces)
        {
            face.Triangulate(ref indicies);
        }
        Indicies = indicies.ToArray();
    }

    public ModelComponent AddComponent<T>(ModelComponent component)
    {
        if(component is null) throw new ArgumentNullException($"Invalid component: {nameof(T)} | {nameof(component)}");
        _Components[typeof(T)] = component;
        component.model = this;
        return component;
    }

    public void RemoveComponent(Type component) => _Components.Remove(component);

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

    void UpdateAllComponents(ModelUpdateType info, object variable)
    {
        foreach (var component in _Components.Values) component.OnModelUpdate(this, info, variable);
    }

    public IEnumerable<(uint,uint,uint)> AllIndicies()
    {
        for(int i = 0; i < Indicies.Length; i+=3)
        {
            yield return (Indicies[i], Indicies[i + 1], Indicies[i + 2]);
        }
    }
}
