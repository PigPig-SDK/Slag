using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace Models;

public class Model
{
    public List<Vertex> Verticies { get; private set; } = [];
    private List<Face> _Faces = [];
    private HashSet<Edge> _Edges = [];

    private Dictionary<Type, ModelComponent> _Components = [];

    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;


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

    public List<uint> GetTriangulatedModel()
    {
        List<uint> indicies = [];
        foreach (Face face in _Faces)
        {
            face.Triangulate(ref indicies);
        }
        return indicies;
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

    void UpdateAllComponents(ModelUpdateType info, object variable)
    {
        foreach (var component in _Components.Values) component.OnModelUpdate(this, info, variable);
    }

    /// <summary>
    /// Note: This objects edges will intentionally be smoothed when triangulated
    /// </summary>
    /// <returns>A basic cube for debugging</returns>
    public static Model InstanceBasicCube()
    {
        Model cube = new();

        //Add verts
        cube.AddVertex(new Vertex(new Vector3(-1, -1, -1), Vector2.Zero));//0
        cube.AddVertex(new Vertex(new Vector3(-1, 1, -1), Vector2.Zero));//1
        cube.AddVertex(new Vertex(new Vector3(1, 1, -1), Vector2.Zero));//2
        cube.AddVertex(new Vertex(new Vector3(1, -1, -1), Vector2.Zero));//3
        cube.AddVertex(new Vertex(new Vector3(-1, 1, 1), Vector2.Zero));//4
        cube.AddVertex(new Vertex(new Vector3(-1, -1, 1), Vector2.Zero));//5
        cube.AddVertex(new Vertex(new Vector3(1, -1, 1), Vector2.Zero));//6
        cube.AddVertex(new Vertex(new Vector3(1, 1, 1), Vector2.Zero));//7

        //AddFace
        cube.AddFace(0, 1, 2, 3);//Left
        cube.AddFace(5, 4, 7, 6);//Right
        cube.AddFace(2, 7, 6, 3);//Front
        cube.AddFace(1, 4, 5, 0);//Back
        cube.AddFace(1, 4, 5, 0);//Back
        cube.AddFace(1, 4, 7, 2);//Top
        cube.AddFace(0, 5, 6, 3);//Bottom

        return cube;
    }
    /// <returns>A basic triangle for debugging</returns>
    public static Model InstanceBasicTriangle()
    {
        Model cube = new();

        //Add verts
        cube.AddVertex(new Vertex(new Vector3(1, 1, -1), Vector2.Zero));//0
        cube.AddVertex(new Vertex(new Vector3(0, 1, -1), Vector2.Zero));//1
        cube.AddVertex(new Vertex(new Vector3(1, 0, -1), Vector2.Zero));//2

        //AddFace
        cube.AddFace(0, 1, 2);

        return cube;
    }
}
