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
        cube.ObjectName = "Cube";

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
        cube.AddFace(0,1,2,3);//Left
        cube.AddFace(6,7,4,5);//Right
        cube.AddFace(2,7,6,3);//Front
        cube.AddFace(0,5,4,1);//Back
        cube.AddFace(1,4,7,2);//Top
        cube.AddFace(3,6,5,0);//Bottom

        return cube;
    }

    /// <returns>A basic cube for debugging</returns>
    public static Model InstanceTorus(int torusIterations, int ringIterations, float torusRadius, float ringRadius)
    {
        Model torus = new();
        torus.ObjectName = "Torus";

        float pi = 3.1415f;

        float torusStep = (2.0f * pi) / (float)torusIterations;
        float ringStep = (2.0f * pi) / (float)ringIterations;

        //The simplest way I could think of accomplishing a torus was using matrix translations.
        for (int iTorus = 0; iTorus < torusIterations; iTorus++)
        {
            Matrix4.CreateRotationY(torusStep * iTorus, out Matrix4 rotationMatrix);
            Matrix4 translation =  Matrix4.CreateTranslation(new Vector3(torusRadius, 0, 0)) * rotationMatrix;
            for (int iRadial = 0; iRadial < ringIterations; iRadial++)
            {
                //build vert position based on local distance
                Vector3 vertPos = new Vector3(MathF.Cos(ringStep * (float)iRadial) * ringRadius, MathF.Sin(ringStep * (float)iRadial) * ringRadius, 0);
                Vector4 posVec4 = new Vector4(vertPos, 1.0f) * translation;
                torus.AddVertex(new Vertex(posVec4.X, posVec4.Y, posVec4.Z));
            }
        }

        //Populating faces...
        for (int t = 0; t < torusIterations; t++)
        {
            for (int r = 0; r < ringIterations; r++)
            {
                int cur = (t * ringIterations) + r;
                int next = (t * ringIterations) + ((r + 1) % ringIterations);
                int curTop = ((t + 1) * ringIterations + r) % torus.Verticies.Count;
                int curTopNext = ((t + 1) * ringIterations + (r + 1) % ringIterations) % torus.Verticies.Count;

                torus.AddFace((uint)curTop, (uint)curTopNext, (uint)next, (uint)cur);
            }
        }
        return torus;
    }

    /// <returns>A basic cone</returns>
    public static Model InstanceCone(int segments, float height, float radius)
    {
        Model cone = new();
        cone.ObjectName = "Cone";

        //Add verts
        cone.AddVertex(new Vertex(0, height, 0));//Top of cone

        float step = 2 * float.Pi / segments;
        for(int i = 0; i < segments; i++)
        {
            cone.AddVertex(new Vertex(MathF.Sin(step * i) * radius, 0, MathF.Cos(step * i) * radius));
        }

        for (uint i = 1; i <= segments; i++)
        {
            uint next = (uint)(i % segments) + 1;//Wrap around.
            cone.AddFace(0, i, next);
        }

        //Bottom
        uint[] bottomIndicies = new uint [segments];
        for(uint i = 0; i < segments; i++)
        {
            bottomIndicies[i] = i + 1;
        }
        cone.AddFace(bottomIndicies);

        return cone;
    }
    /// <returns>A basic triangle for debugging</returns>
    public static Model InstanceBasicTriangle()
    {
        Model triangle = new();
        triangle.ObjectName = "Triangle";

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
        triad.ObjectName = "Triad";

        float arrowLength = 0.5f;
        float arrowWidth = 0.025f;

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
        triad.ObjectName = "DebugLine";

        triad.AddVertex(new Vertex(start.X, start.Y, start.Z));
        triad.AddVertex(new Vertex(start.X, start.Y + 0.1f, start.Z));
        triad.AddVertex(new Vertex(end.X, end.Y + 0.1f, end.Z));
        triad.AddVertex(new Vertex(end.X, end.Y, end.Z));
        triad.AddFace(0, 1, 2, 3);

        return triad;
    }
    /// <returns>Returns a edge only object</returns>
    public static Model DebugEdges()
    {
        Model triad = new Model();
        triad.ObjectName = "DebugEdges";

        triad.AddVertex(new Vertex(0,1,0));//0
        triad.AddVertex(new Vertex(0, 0, 0));//1
        triad.AddVertex(new Vertex(0, 0, 1));//2
        triad.AddVertex(new Vertex(1, 0, 0));//3
        triad.AddVertex(new Vertex(0, 1, 0));//4

        triad.AddEdge(new Edge(0, 1));
        triad.AddEdge(new Edge(1, 2));
        triad.AddEdge(new Edge(2, 0));

        return triad;
    }
}
