using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

/// <summary>
/// A polygon edge
/// 
/// Note: Vertex1 will always be a lower index than Vertex2. This is to simplify equality.
/// </summary>
public class Edge
{
    public uint Vertex1 { get; internal set; }
    public uint Vertex2 { get; internal set; }
    public bool IsSharp = false;

    public List<Face> Faces { get; set; } = [];

    public Edge(uint vertex1, uint vertex2, params Face[] faces) : this(vertex1, vertex2)
    {
        Faces = new (faces);
    }

    public Edge(uint vertex1, uint vertex2)
    {
        //Enforce vertex 1 is a lower index than vertex2
        if (vertex1 > vertex2)
        {
            //swap vertex 1 and vertex 2 
            vertex1 ^= vertex2;
            vertex2 ^= vertex1;
            vertex1 ^= vertex2;
        }

        Vertex1 = vertex1;
        Vertex2 = vertex2;
    }

    public (uint vertex1, uint vertex2) AsTuple() => (Vertex1, Vertex2);

    public bool Contains(int index) => (Vertex1 == index || Vertex2 == index);

    public bool RequiresDecrement(int index) => (Vertex1 > 1) || (Vertex2 > index);

    internal void DecrementForIndex(int index)
    {
        if (Vertex1 > index) Vertex1--;
        if (Vertex2 > index) Vertex2--;
    }

    public override bool Equals(object? obj)
    {
        if(obj is Edge edge)
        {
            return edge.Vertex1 == Vertex1 && edge.Vertex2 == Vertex2;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return (EqualityComparer<uint>.Default.GetHashCode(Vertex1)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(Vertex1);

    }
}
