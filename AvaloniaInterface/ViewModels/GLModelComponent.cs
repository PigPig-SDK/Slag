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
    private int? _VertexArrayObject;

    private int _IndiciesCount = 0;

    GlInterface? glInterface = null;

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
        if(_IndiciesBuffer != null && _VertexBufferObject != null)
        {
            throw new InvalidOperationException($"Accidentally tried to assign {GetInvalidBuffer()} before setting null!");
        }
        glInterface = gl;

        //Setup VAO
        _VertexArrayObject = gl.GenVertexArray();
        gl.BindVertexArray(_VertexArrayObject!.Value);
        Console.WriteLine($"{nameof(_VertexArrayObject)} Error: {gl.GetError()}");

        //Setup VBO
        _VertexBufferObject = gl.GenBuffer();
        Console.WriteLine($"{nameof(_VertexBufferObject)} Error: {gl.GetError()}");

        //Setup EBO
        _IndiciesBuffer = gl.GenBuffer();
        Console.WriteLine($"{nameof(_IndiciesBuffer)} Error: {gl.GetError()}");

        //Upload vertex information to the VBO and EBO
        UpdateBuffers(gl);

        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        Console.WriteLine($"{nameof(_VertexBufferObject)} Bind Error: {gl.GetError()}");

        //Setup location info inside VAO
        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, Vertex.GetSize(), 0);
        gl.EnableVertexAttribArray(0);
        Console.WriteLine($"Attrib0 Error: {gl.GetError()}");


        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("Normal"));
        gl.EnableVertexAttribArray(1);
        Console.WriteLine($"Attrib1 Error: {gl.GetError()}");

        gl.VertexAttribPointer(2, 2, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("UV"));
        gl.EnableVertexAttribArray(2);
        Console.WriteLine($"Attrib2 Error: {gl.GetError()}");
    }

    public unsafe void UpdateBuffers(GlInterface gl)
    {
        //Temp buffers, to be sent to GPU.
        Vertex[] verts = [];

        //Compute model data.
        model.GenerateIndicies();
        verts = model.Verticies.ToArray();
        ComputeNormals(verts, model.Indicies);
        _IndiciesCount = model.Indicies.Length;



        //Inform of vert data
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        fixed (Vertex* ptr = verts)
        {
            gl.BufferData(GL_ARRAY_BUFFER, Vertex.GetSize() * verts.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(verts)} Upload Error: {gl.GetError()}");

        //Inform of indicies
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _IndiciesBuffer!.Value);
        fixed (uint* ptr = model.Indicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * _IndiciesCount, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(model.Indicies)} Upload Error: {gl.GetError()}");
    }

    public unsafe void RenderModel(GlInterface gl, int modelMatrixUniform)
    {
        if (model.Hidden) return;//Do not render.

        if (_VertexBufferObject == null || _IndiciesBuffer == null)
        {
            Console.WriteLine($"Tried to render object while : {GetInvalidBuffer()} is null! Discarded draw call.");
            return;
        }

        Matrix4 rotX = Matrix4.CreateRotationX(model.Rotation.X);
        Matrix4 rotY = Matrix4.CreateRotationY(model.Rotation.Y);
        Matrix4 rotZ = Matrix4.CreateRotationZ(model.Rotation.Z);
        Matrix4 rotationMat = rotZ * rotY * rotX; // ZYX order of rotation...

        Matrix4 modelTransformation = 
            Matrix4.CreateTranslation(model.Position) 
            * rotationMat 
            * Matrix4.CreateScale(model.Scale);

        gl.UniformMatrix4fv(modelMatrixUniform, 1, false, &modelTransformation);

        gl.BindVertexArray(_VertexArrayObject!.Value);
        //Console.WriteLine($"{nameof(_VertexArrayObject)} Bind Error: {gl.GetError()}");

        //Render triangles
        gl.DrawElements(GL_TRIANGLES, _IndiciesCount, GL_UNSIGNED_INT, 0);
        //Console.WriteLine($"{nameof(_Indicies)} Draw error: {gl.GetError()}");
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
        for(int i = 0; i < indicies.Length; i+=3)
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

    public static IEnumerable<GLModelComponent> AllComponents(List<Model> models)
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
        if (glInterface == null) return;

        glInterface.DeleteBuffer(_VertexBufferObject!.Value);
        glInterface.DeleteBuffer(_IndiciesBuffer!.Value);
        glInterface.DeleteVertexArray(_VertexArrayObject!.Value);
    }
}
