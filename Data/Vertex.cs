using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Models;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex(Vector3 position, Vector2 uv)
{
    public Vector3 Position = position;
    public Vector2 UV = uv;
    public Vector3 Normal = Vector3.Zero;//On the model to generate normals.
    public static int GetSize()
    {
        unsafe
        {
            return sizeof(Vector3) + sizeof(Vector2) + sizeof(Vector3);
        }
    }
}
