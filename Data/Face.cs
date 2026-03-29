namespace Core;

/// <summary>
/// The ParentModel property is ignored in hashcode and Equals
/// </summary>
/// <param name="indicies">The indicies which define a face. The data for the indicies is stored within ParentModel</param>
public class Face : ICloneable
{
    public List<uint> Indicies;

    public List<Edge> Edges { get; set; } = [];

    public Model? ParentModel = null;

    public Face(List<uint> indicies)
    {
        Indicies = indicies;
    }

    public Face(params uint[] values)
    {
        Indicies = [.. values];
    }

    public override bool Equals(object? obj)
    {
        return obj is Face face && EqualityComparer<List<uint>>.Default.Equals(Indicies, face.Indicies);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Indicies);
    }
    
    public void Flip() => Indicies.Reverse();

    public bool Contains(uint index) => Indicies.Contains(index);

    public void DecrementForIndex(int index)
    {
        for (int i = 0; i < Indicies.Count; i++)
        {
            if(Indicies[i] > index) Indicies[i]--;
        }
    }

    public void Triangulate(ref List<uint> list)
    {
        FanTriangulate(ref list);
    }

    private void FanTriangulate(ref List<uint> list)
    {
        uint fanSource = Indicies.First();
        for(int i = 1; i < Indicies.Count - 1; i++)
        {
            list.Add(fanSource);
            list.Add(Indicies[i]);
            list.Add(Indicies[i + 1]);
            if(ParentModel is not null)
            {
                var tuple = (fanSource, Indicies[i], Indicies[i + 1]);

                if (ParentModel.TriangleToFaceMapping.ContainsKey(tuple))
                {
                    if (ParentModel.TriangleToFaceMapping[tuple] == this)
                        Console.WriteLine("Triangle already exists, face is the same. Interesting.");
                    else
                        Console.WriteLine($"Triangle already exists, face is not the same. { ParentModel.ObjectName }");

                        continue;
                }
                ParentModel.TriangleToFaceMapping.Add(tuple, this);
            }
        }
    }
    /// <summary>
    /// Deep copy, Does not copy Edges array.
    /// </summary>
    public object Clone()
    {
        return new Face(Indicies.ToArray());
    }
}
