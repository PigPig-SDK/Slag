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
public record class Edge
{
    public uint Vertex1 { get; internal set; }
    public uint Vertex2 { get; internal set; }

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

    public bool Contains(int index) => (Vertex1 == index || Vertex2 == index);

    internal void DecrementForIndex(int index)
    {
        if (Vertex1 > index) Vertex1--;
        if (Vertex2 > index) Vertex2--;
    }
}
