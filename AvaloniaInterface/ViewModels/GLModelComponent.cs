using Avalonia.OpenGL;
using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;
using static OpenglAvaloniaTest.ViewModels.GlConstantsExtended;

namespace OpenglAvaloniaTest.ViewModels;

public class GLModelComponent : ModelComponent
{
    private int? _VertexBufferObject = null;
    private int? _IndiciesBuffer = null;
    private int? _EdgeIndiciesBuffer = null;

    private int? _TriangleArrayObject;
    private int? _EdgeArrayObject;

    private int _IndiciesCount = 0;
    private int _VertexCount = 0;
    private int _EdgeIndiciesCount = 0;

    private GlInterface? glInterface = null;

    private Dictionary<uint, List<uint>> _SharpIndicies = [];


    public void OpenglRestart(GlInterface gl)
    {
        //Clear buffers
        _IndiciesBuffer = null;
        _VertexBufferObject = null;
        GenerateBuffers(gl);
    }

    private string GetInvalidBuffer()
    {
        return $"{(_VertexBufferObject == null ? $"{nameof(_VertexBufferObject)}, " : null)}{(_IndiciesBuffer == null ? $"{nameof(_IndiciesBuffer)}" : null)}";
    }

    public void GenerateBuffers(GlInterface gl)
    {
        if (_IndiciesBuffer != null && _VertexBufferObject != null)
        {
            throw new InvalidOperationException($"Accidentally tried to assign {GetInvalidBuffer()} before setting null!");
        }
        glInterface = gl;

        //Setup VAO
        _TriangleArrayObject = gl.GenVertexArray();
        gl.BindVertexArray(_TriangleArrayObject!.Value);
        //Setup buffers
        _VertexBufferObject = gl.GenBuffer();
        _IndiciesBuffer = gl.GenBuffer();
        UpdateTrangleBuffers(gl);
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        //Setup data location info inside VAO
        SetLocationsInsideVAO(gl);

        //Setup edge VAO
        _EdgeArrayObject = gl.GenVertexArray();
        _EdgeIndiciesBuffer = gl.GenBuffer();
        gl.BindVertexArray(_EdgeArrayObject!.Value);
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _EdgeIndiciesBuffer!.Value);
        //Setup data location info inside VAO
        SetLocationsInsideVAO(gl);
        UpdateEdgeBuffers(gl);

        //Setup vertex VAO
    }

    private void SetLocationsInsideVAO(GlInterface gl)
    {
        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, Vertex.GetSize(), 0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("Normal"));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 2, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("UV"));
        gl.EnableVertexAttribArray(2);
    }

    public unsafe void UpdateEdgeBuffers(GlInterface gl)
    {
        uint[] edgeIndicies = model.GetEdgeIndicies();

        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _EdgeIndiciesBuffer!.Value);
        fixed (uint* ptr = edgeIndicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * edgeIndicies.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(edgeIndicies)} Upload Error: {gl.GetError()}");
        _EdgeIndiciesCount = edgeIndicies.Length;
    }

    public unsafe void UpdateTrangleBuffers(GlInterface gl)
    {
        Vertex[] verts = [];
        List<uint> indiciesList = []; //List that will encounter lots of modification
        _SharpIndicies.Clear();

        model.GenerateTriangulatedModel(ref verts, ref indiciesList);

        //Recalculates the model to account for 'sharp' edges
        ComputeSmoothing(ref indiciesList);

        uint[] indicies = indiciesList.ToArray();

        //Manage edges
        ComputeNormals(verts, indicies);
        _IndiciesCount = indicies.Length;
        _VertexCount = verts.Length;

        ///
        //Populate OPENGL buffers
        ///

        //Inform of vert data
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        fixed (Vertex* ptr = verts)
        {
            gl.BufferData(GL_ARRAY_BUFFER, Vertex.GetSize() * verts.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(verts)} Upload Error: {gl.GetError()}");

        //Inform of indicies
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _IndiciesBuffer!.Value);
        fixed (uint* ptr = indicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * indicies.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(indicies)} Upload Error: {gl.GetError()}");
    }

    /// <summary>
    /// Accounts for sharp edges in the model, creating duplicate verticies for each smoothing group.
    /// </summary>
    private void ComputeSmoothing(ref List<uint> indiciesList)
    {
        //Not implemented yet.
    }

    public unsafe void RenderModel(GlInterface gl)
    {
        if (_VertexBufferObject == null || _IndiciesBuffer == null)
        {
            Console.WriteLine($"Tried to render object while : {GetInvalidBuffer()} is null! Discarded draw call.");
            return;
        }
        gl.DepthMask(1);//true
        gl.DepthFunc(GL_LESS);
        gl.BindVertexArray(_TriangleArrayObject!.Value);
        gl.DrawElements(GL_TRIANGLES, _IndiciesCount, GL_UNSIGNED_INT, 0);
    }

    internal void RenderEdges(GlInterface gl)
    {
        gl.DepthFunc(GL_LEQUAL);
        gl.DepthMask(0);//false
        gl.BindVertexArray(_EdgeArrayObject!.Value);
        gl.DrawElements(GL_LINES, _EdgeIndiciesCount, GL_UNSIGNED_INT, 0);
        gl.BindVertexArray(0);
    }

    internal void RenderVerts(GlInterface gl)
    {
        gl.BindVertexArray(_TriangleArrayObject!.Value);
        gl.DrawArrays(GL_POINTS, 0, _VertexCount);
        gl.BindVertexArray(0);
    }

    public void ComputeNormals(Vertex[] verts, uint[] indicies)
    {
        if (indicies.Length % 3 != 0) throw new ArgumentException($"{nameof(indicies)} must be a multiple of 3.");

        //Reset normals
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].Normal = new Vector3(0, 0, 0);
        }

        //Compute sum of normals
        for (int i = 0; i < indicies.Length; i += 3)
        {
            //Get locations
            Vector3 p1 = verts[indicies[i]].Position;
            Vector3 p2 = verts[indicies[i + 1]].Position;
            Vector3 p3 = verts[indicies[i + 2]].Position;

            Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1);

            verts[indicies[i]].Normal += normal;
            verts[indicies[i + 1]].Normal += normal;
            verts[indicies[i + 2]].Normal += normal;
        }

        //Normalize
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].Normal = Vector3.Normalize(verts[i].Normal);
        }
    }

    public override void OnModelUpdate(Model model, ModelUpdateType info, object data)
    {

    }

    public static IEnumerable<GLModelComponent> AllComponents(IEnumerable<Model> models)
    {
        foreach (Model model in models)
        {
            if (!model.TryGetComponent<GLModelComponent>(out ModelComponent? component))//Add component it DNE!
            {
                //component = model.AddComponent<GLModelComponent>(new GLModelComponent());
                continue;
            }
            yield return (GLModelComponent)component!;
        }
    }

    public override void Dispose()
    {
        //Clean up the opengl resources.
        if (glInterface == null) return;

        if (_VertexBufferObject != null) glInterface.DeleteBuffer(_VertexBufferObject!.Value);
        if (_IndiciesBuffer != null) glInterface.DeleteBuffer(_IndiciesBuffer!.Value);
        if (_TriangleArrayObject != null) glInterface.DeleteVertexArray(_TriangleArrayObject!.Value);

        glInterface = null;
    }

    internal Matrix4 GetModelTranslationMatrix()
    {
        Matrix4 rotX = Matrix4.CreateRotationX(model.Rotation.X);
        Matrix4 rotY = Matrix4.CreateRotationY(model.Rotation.Y);
        Matrix4 rotZ = Matrix4.CreateRotationZ(model.Rotation.Z);
        Matrix4 rotationMat = rotZ * rotY * rotX; // ZYX order of rotation...

        return rotationMat
            * Matrix4.CreateTranslation(model.Position)
            * Matrix4.CreateScale(model.Scale);
    }
}
