namespace Core;

public class EdgeHit
{
    public Edge Edge { get; set; }
    public Model Model { get; set; }

    public EdgeHit(Edge edge, Model model)
    { 
        this.Edge = edge;
        this.Model = model;
    }

}