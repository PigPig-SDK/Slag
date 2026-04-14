namespace Core;

public class VertexHit
{
    public Model Model { get; set; }
    public uint VertexIndex { get; set; }
    public float Distance { get; set; }

    public VertexHit(Model model, uint vertexIndex, float distance)
    {
        Model = model;
        VertexIndex = vertexIndex;
        Distance = distance;
    }
}
