using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

[Flags]
public enum RenderMode
{
    Triangles = 1 << 0,
    Edges = 1 << 1,
    Verts = 1 << 2,
    Solid = Triangles | Verts | Edges,
    Wireframe = Edges | Verts,
}
