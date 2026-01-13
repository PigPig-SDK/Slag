using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Models;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public Vector3 Position = Vector3.Zero;
    public Vector2 UV = Vector2.Zero;
    public Vector3 Normal = Vector3.Zero;//On the model to generate normals.

    public Vertex(Vector3 position, Vector2 uv) 
    { 
        Position = position;
        UV = uv;
    }
    public Vertex(params float[] paramPositions)
    {
        if(paramPositions == null || paramPositions.Length != 3)
            throw new ArgumentException($"{nameof(paramPositions)} was of invalid length, Please only give 3 params!");

        Position.X = paramPositions[0];
        Position.Y = paramPositions[1];
        Position.Z = paramPositions[2];
    }

    public static int GetSize()
    {
        unsafe
        {
            return sizeof(Vector3) + sizeof(Vector2) + sizeof(Vector3);
        }
    }
}
