using OpenTK.Mathematics;

namespace Core;

public static partial class ModelPrefabs
{
    /// <summary>
    /// Note: This objects edges will intentionally be smoothed when triangulated
    /// </summary>
    /// <returns>A basic cube for debugging</returns>
    public static Model Cube()
    {
        Model cube = new()
        {
            ObjectName = "Cube"
        };

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

    public static Model Plane(float size, string name = "Plane")
    {
        Model plane = new();
        plane.ObjectName = name;

        //Add verts
        plane.AddVertex(new Vertex(new Vector3(-size, 0, -size), new Vector2(0, 0))); // 0
        plane.AddVertex(new Vertex(new Vector3(-size, 0, size), new Vector2(0,1))); // 1
        plane.AddVertex(new Vertex(new Vector3(size, 0, size), new Vector2(1, 1))); // 2
        plane.AddVertex(new Vertex(new Vector3(size, 0, -size), new Vector2(1, 0))); // 3

        

        //AddFace
        plane.AddFace(0, 1, 2, 3);//Left

        return plane;
    }
    public static Model BBoxVisualizer(Vector3 start, Vector3 end)
    {
        Model bboxVisualizer = new()
        {
            ObjectName = "VISUALIZER"
        };

        //Add verts
        bboxVisualizer.AddVertex(new Vertex(end.X, end.Y, end.Z)); // 0
        bboxVisualizer.AddVertex(new Vertex(end.X, start.Y, end.Z)); // 1
        bboxVisualizer.AddVertex(new Vertex(start.Y, start.Y, end.Z)); // 2
        bboxVisualizer.AddVertex(new Vertex(start.Y, end.Y, end.Z)); // 3
        bboxVisualizer.AddVertex(new Vertex(end.X, start.Y, start.Z)); // 4
        bboxVisualizer.AddVertex(new Vertex(end.X, end.Y, start.Z)); // 5
        bboxVisualizer.AddVertex(new Vertex(start.X, end.Y, start.Z)); // 6
        bboxVisualizer.AddVertex(new Vertex(start.X, start.Y, start.Z)); // 7

        //AddFace
        bboxVisualizer.AddFace(0, 1, 2, 3);//Left
        bboxVisualizer.AddFace(6, 7, 4, 5);//Right
        bboxVisualizer.AddFace(2, 7, 6, 3);//Front
        bboxVisualizer.AddFace(0, 5, 4, 1);//Back
        bboxVisualizer.AddFace(1, 4, 7, 2);//Top
        bboxVisualizer.AddFace(3, 6, 5, 0);//Bottom

        return bboxVisualizer;
    }

    /// <returns>A basic cube for debugging</returns>
    public static Model Torus(int torusIterations, int ringIterations, float torusRadius, float ringRadius)
    {
        Model torus = new()
        {
            ObjectName = "Torus"
        };

        const float pi = MathF.PI;

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
                Vector3 vertPos = new(MathF.Cos(ringStep * (float)iRadial) * ringRadius, MathF.Sin(ringStep * (float)iRadial) * ringRadius, 0);
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

    public static Model Sphere(uint step, float radius)
    {
        Model circle = new()
        {
            ObjectName = "Circle"
        };

        //Probably better ways of accomplishing this.

        for (float b = -step / 2.0f; b <= step / 2.0f; b++)
        {
            if (b == step / 2.0f)//Top
            {
                circle.AddVertex(new Vertex(0, radius, 0));
            }
            else if (b == -step / 2.0f)//Bottom
            {
                circle.AddVertex(new Vertex(0, -radius, 0));
            }
            else//Rings
            {
                for (int a = 0; a < step; a++)
                {
                    float phi = (((float)a / (float)step)) * 2 * MathF.PI;
                    float theta = (((float)b / (float)step)) * MathF.PI;
                    circle.AddVertex(new Vertex(
                        MathF.Cos(theta) * MathF.Sin(phi) * radius,
                        MathF.Sin(theta) * radius,
                        MathF.Cos(theta) * MathF.Cos(phi) * radius));
                }
            }
        }

        for (uint longitude = 0; longitude < step; longitude++)
        {
            if (longitude == 0)//Bottom ring
            {
                //Scan the ring above us.
                for (uint ringIndex = 0; ringIndex < step; ringIndex++)
                {
                    uint index = ringIndex + 1;
                    uint indexNext = (index % step) + 1;
                    circle.AddFace(indexNext, index , 0);

                }
            }
            else if (longitude == step - 1)// Top ring
            {
                uint rootIndex = (uint)circle.Verticies.Count - 1;
                for (uint ringIndex = 0; ringIndex < step; ringIndex++)
                {
                    uint index = ringIndex + 1;
                    uint indexNext = (index % step) + 1;
                    circle.AddFace(rootIndex - indexNext, rootIndex - index, rootIndex);
                }
            }
            else//Body rings
            {
                for (uint latitude = 0; latitude < step; latitude++)
                {
                    uint cur = ((longitude - 1) * step) + latitude + 1;
                    uint next = ((longitude - 1) * step) + ((latitude + 1) % step) + 1;
                    uint curTop = ((longitude) * step) + latitude + 1;
                    uint curTopNext = ((longitude) * step) + ((latitude + 1) % step) + 1;
                    circle.AddFace(next, curTopNext, curTop, cur);
                }
            }
        }

        return circle;

    }

    /// <returns>A cylinder</returns>
    public static Model Cylinder(float height, float radius, int triangulation)
    {
        Model cylinder = new()
        {
            ObjectName = "Cylinder"
        };

        float step = (2.0f * MathF.PI) / triangulation;

        for (int i = 0; i < triangulation; i++)
        {
            //Top first, followed by bottom
            cylinder.AddVertex(new Vertex(MathF.Sin(step * i), height/2, MathF.Cos(step * i)));
            cylinder.AddVertex(new Vertex(MathF.Sin(step * i), -height/2, MathF.Cos(step * i)));
        }

        for (uint i = 0; i < cylinder.Verticies.Count; i += 2)
        {
            uint top = i;
            uint bottom = i + 1;
            uint next = (uint)((i + 2) % cylinder.Verticies.Count);
            uint nextTop = next;
            uint nextBottom = next + 1;
            cylinder.AddFace(bottom, nextBottom, nextTop, top);
        }

        uint[] topFace = new uint[triangulation];
        uint[] bottomFace = new uint[triangulation];

        for (uint i = 0; i < triangulation; i++)
        {
            topFace[i] = (i * 2);
            bottomFace[i] = (i * 2) + 1;
        }

        cylinder.AddFace(bottomFace.Reverse().ToArray());
        cylinder.AddFace(topFace);

        return cylinder;
    }

    /// <returns>A basic cone</returns>
    public static Model Cone(int segments, float height, float radius)
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
        uint[] bottomIndices = new uint [segments];
        for(uint i = 0; i < segments; i++)
        {
            bottomIndices[i] = i + 1;
        }
        bottomIndices = bottomIndices.Reverse().ToArray();
        cone.AddFace(bottomIndices);

        return cone;
    }
    /// <returns>A basic triangle for debugging</returns>
    public static Model Triangle()
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
    public static Model AxisTriad()
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

        return triad;
    }
}
