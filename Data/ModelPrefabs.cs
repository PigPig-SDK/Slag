using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Models;

public static class ModelPrefabs
{

    /// <summary>
    /// Note: This objects edges will intentionally be smoothed when triangulated
    /// </summary>
    /// <returns>A basic cube for debugging</returns>
    public static Model InstanceBasicCube()
    {
        Model cube = new();
        cube.ObjectName = "Generated Cube";

        //Add verts
        cube.AddVertex(new Vertex(-1, -1, -1)); // 0
        cube.AddVertex(new Vertex(-1, 1, -1)); // 1
        cube.AddVertex(new Vertex(1, 1, -1)); // 2
        cube.AddVertex(new Vertex(1, -1, -1)); // 3
        cube.AddVertex(new Vertex(-1, 1, 1)); // 4
        cube.AddVertex(new Vertex(-1, -1, 1)); // 5
        cube.AddVertex(new Vertex(1, -1, 1)); // 6
        cube.AddVertex(new Vertex(1, 1, 1)); // 7

        //AddFace
        cube.AddFace(0, 1, 2, 3);//Left
        cube.AddFace(5, 4, 7, 6);//Right
        cube.AddFace(2, 7, 6, 3);//Front
        cube.AddFace(1, 4, 5, 0);//Back
        cube.AddFace(1, 4, 7, 2);//Top
        cube.AddFace(0, 5, 6, 3);//Bottom

        return cube;
    }
    /// <returns>A basic triangle for debugging</returns>
    public static Model InstanceBasicTriangle()
    {
        Model triangle = new();
        triangle.ObjectName = "Basic Triangle";

        //Add verts
        triangle.AddVertex(new Vertex(1, 1, -1));//0
        triangle.AddVertex(new Vertex(0, 1, -1));//1
        triangle.AddVertex(new Vertex(1, 0, -1));//2

        //AddFace
        triangle.AddFace(0, 1, 2);

        return triangle;
    }
    /// <returns>An XYZ axis triad shape for debugging</returns>
    public static Model InstanceAxisTriad()
    {
        Model triad = new Model();
        triad.ObjectName = "Axis Triad model";

        float arrowLength = 2.0f;
        float arrowWidth = 0.05f;

        triad.AddVertex(new Vertex(0, 0, 0));
        triad.AddVertex(new Vertex(arrowLength, arrowWidth, 0));
        triad.AddVertex(new Vertex(arrowLength, -arrowWidth, 0));
        triad.AddFace(0, 1, 2);

        triad.AddVertex(new Vertex(-arrowWidth, arrowLength, 0));
        triad.AddVertex(new Vertex(arrowWidth, arrowLength, 0));
        triad.AddFace(0, 3, 4);

        triad.AddVertex(new Vertex(-arrowWidth, 0, arrowLength));
        triad.AddVertex(new Vertex(arrowWidth, 0, arrowLength));
        triad.AddFace(0, 5, 6);

        return triad;
    }
    /// <returns>An XYZ axis triad shape for debugging</returns>
    public static Model DebugTriangleLine(Vector3 start, Vector3 end)
    {
        Model triad = new Model();
        triad.ObjectName = "Debugging model";

        triad.AddVertex(new Vertex(start.X, start.Y, start.Z));
        triad.AddVertex(new Vertex(start.X, start.Y + 0.1f, start.Z));
        triad.AddVertex(new Vertex(end.X, end.Y + 0.1f, end.Z));
        triad.AddVertex(new Vertex(end.X, end.Y, end.Z));
        triad.AddFace(0, 1, 2, 3);

        return triad;
    }
}
