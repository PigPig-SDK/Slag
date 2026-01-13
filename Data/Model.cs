using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using OpenTK.Mathematics;

namespace Models;

public class Model
{
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
        cube.AddVertex(new Vertex(-1, -1, -1)); // 0
        cube.AddVertex(new Vertex(-1, 1, -1)); // 1
        cube.AddVertex(new Vertex(1, 1, -1)); // 2
        cube.AddVertex(new Vertex(1, -1, -1)); // 3
        cube.AddVertex(new Vertex(-1, 1, 1)); // 4
        cube.AddVertex(new Vertex(-1, -1, 1)); // 5
        cube.AddVertex(new Vertex(1, -1, 1)); // 6
        cube.AddVertex(new Vertex(1, 1, 1)); // 7

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
        Model triangle = new();

        //Add verts
        triangle.AddVertex(new Vertex(1, 1, -1));//0
        triangle.AddVertex(new Vertex(0, 1, -1));//1
        triangle.AddVertex(new Vertex(1, 0, -1));//2

        //AddFace
        triangle.AddFace(0, 1, 2);

        return triangle;
    }
    /// <returns>An XYZ axis triad shape for debugging</returns>
    public static Model InstanceAxisTriad()
    {
        Model triad = new Model();

        float arrowLength = 2.0f;
        float arrowWidth = 0.05f;

        triad.AddVertex(new Vertex(0, 0, 0));
        triad.AddVertex(new Vertex(arrowLength, arrowWidth, 0));
        triad.AddVertex(new Vertex(arrowLength, -arrowWidth, 0));
        triad.AddFace(0, 1, 2);

        triad.AddVertex(new Vertex(-arrowWidth, arrowLength, 0));
        triad.AddVertex(new Vertex(arrowWidth, arrowLength, 0));
        triad.AddFace(0, 3, 4);

        triad.AddVertex(new Vertex(-arrowWidth, 0, arrowLength));
        triad.AddVertex(new Vertex(arrowWidth, 0, arrowLength));
        triad.AddFace(0, 5, 6);

        return triad;
    }

}
