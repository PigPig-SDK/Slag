namespace OpenglAvaloniaTest.ViewModels;

[System.Flags]
public enum RenderMode
{
    Triangles = 1 << 0,
    Edges = 1 << 1,
    Verts = 1 << 2,
    Depth = 1 << 3,
    Solid = Triangles | Verts | Edges,
    Wireframe = Edges | Verts,
}
