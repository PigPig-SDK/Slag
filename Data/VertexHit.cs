namespace Models;

public class VertexHit
{
    public Model Model;
    public uint VertexIndex;
    public float Distance;

    public VertexHit(Model model, uint vertexIndex, float distance)
    {
        Model = model;
        VertexIndex = vertexIndex;
        Distance = distance;
    }
}
