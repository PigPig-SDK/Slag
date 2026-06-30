using OpenTK.Mathematics;

namespace Core;

/// <summary>
/// The ParentModel property is ignored in hashcode and Equals
/// </summary>
/// <param name="indices">The indices which define a face. The data for the indices is stored within ParentModel</param>
public class Face : ICloneable
{
    public List<uint> Indices { get; }

    public HashSet<Edge> Edges { get; private set; } = [];

    public Model? ParentModel { get; set; }

    public Face(List<uint> indices)
    {
        Indices = indices;
    }

    public Face(params uint[] values)
    {
        Indices = [.. values];
    }

    public override bool Equals(object? obj)
    {
        return obj is Face face && EqualityComparer<List<uint>>.Default.Equals(Indices, face.Indices);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Indices);
    }
    
    public void Flip() => Indices.Reverse();

    public bool Contains(uint index) => Indices.Contains(index);

    public void DecrementForIndex(int index)
    {
        for (int i = 0; i < Indices.Count; i++)
        {
            if(Indices[i] > index) Indices[i]--;
        }
    }

    public void Triangulate(ref List<uint> list)
    {
        FanTriangulate(ref list);
    }

    private void FanTriangulate(ref List<uint> list)
    {
        uint fanSource = Indices.First();
        for(int i = 1; i < Indices.Count - 1; i++)
        {
            list.Add(fanSource);
            list.Add(Indices[i]);
            list.Add(Indices[i + 1]);
            if(ParentModel is not null)
            {
                var tuple = (fanSource, Indices[i], Indices[i + 1]);

                if (ParentModel.TriangleToFaceMapping.ContainsKey(tuple))
                {
                    if (ParentModel.TriangleToFaceMapping[tuple] == this)
                        Console.WriteLine("Triangle already exists, face is the same. Interesting.");
                    else
                        Console.WriteLine($"Triangle already exists, face is not the same. { ParentModel.ObjectName }");

                        continue;
                }
                ParentModel.AddFaceMapping(tuple, this);
            }
        }
    }
    /// <summary>
    /// Deep copy, Does not copy Edges array.
    /// </summary>
    public object Clone()
    {
        return new Face(Indices.ToArray());
    }
    public override string ToString()
    {
        return $"Indices : {string.Join(",", Indices)}\nEdges : {string.Join(",", Edges)}";
    }

    public Vector3 GetNormal()
    {
        if(ParentModel is null) return Vector3.Zero;

        Vector3 normal = Vector3.Zero;
        foreach(uint vertex in Indices)
        {
            if(ParentModel.GetVertex(vertex).Normal != Vector3.Zero)
            {
                normal += ParentModel.GetVertex(vertex).Normal;
            }
        }
        return normal.Normalized();
    }
}
