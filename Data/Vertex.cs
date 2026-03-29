using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Core;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position = Vector3.Zero;
    public Vector2 UV = Vector2.Zero;
    public Vector3 Normal = Vector3.Zero;//On the model to generate normals.

    public Vertex(Vertex vertex) : this()
    {
        Position = vertex.Position;
        UV = vertex.UV;
        Normal = vertex.Normal;
    }

    public Vertex(Vector3 position, Vector2 uv) 
    { 
        Position = position;
        UV = uv;
    }
    public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        UV = uv;
        Normal = normal;
    }
    public Vertex(float x, float y, float z)
    {
        Position.X = x;
        Position.Y = y;
        Position.Z = z;
    }

    public static int GetSize()
    {
        unsafe
        {
            return sizeof(Vector3) + sizeof(Vector2) + sizeof(Vector3);
        }
    }
}
