using Avalonia.OpenGL;
using Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;
using static OpenglAvaloniaTest.ViewModels.GlConstantsExtended;

namespace OpenglAvaloniaTest.ViewModels;

public class GLModelComponent : ModelComponent
{
    private int? _VertexBufferObject = null;
    private int? _IndiciesBuffer = null;
    private int? _VertexArrayObject;

    private Vertex[] _Verts = [];
    private uint[] _Indicies = [];
    

    public void OpenglRestart(GlInterface gl)
    {
        //Clear buffers
        _IndiciesBuffer = null;
        _VertexBufferObject = null;
        GenerateBuffers(gl);
    }

    private string getInvalidBuffer()
    {
        return $"{(_VertexBufferObject == null ? $"{nameof(_VertexBufferObject)}, " : null)}{(_IndiciesBuffer == null ? $"{nameof(_IndiciesBuffer)}" : null)}";
    }

    public void GenerateBuffers(GlInterface gl)
    {
        if(_IndiciesBuffer != null && _VertexBufferObject != null)
        {
            throw new InvalidOperationException($"Accidentally tried to assign {getInvalidBuffer()} before setting null!");
        }

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

        //Setup location info inside VAO
        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, Vertex.GetSize(), 0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("Normal"));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 2, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("UV"));
        gl.EnableVertexAttribArray(2);
    }

    public unsafe void UpdateBuffers(GlInterface gl)
    {
        //Inform of vert data
        _Verts = model.Verticies.ToArray();
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);
        fixed (Vertex* ptr = _Verts)
        {
            gl.BufferData(GL_ARRAY_BUFFER, Vertex.GetSize() * _Verts.Length, (nint)ptr, GL_STATIC_DRAW);
        }

        //Inform of indicies
        PopulateTriangulatedIndicies();
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _IndiciesBuffer!.Value);
        fixed (uint* ptr = _Indicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * _Indicies.Length, (nint)ptr, GL_STATIC_DRAW);
        }
    }

    public void RenderModel(GlInterface gl)
    {
        if (_VertexBufferObject == null || _IndiciesBuffer == null)
        {
            Console.WriteLine($"Tried to render object while : {getInvalidBuffer()} is null! Discarded draw call.");
            return;
        }

        gl.BindVertexArray(_VertexArrayObject!.Value);
        //Render triangles
        gl.DrawElements(GL_TRIANGLES, _Indicies.Length, GL_UNSIGNED_INT, 0);
        gl.BindVertexArray(0);
    }

    public void PopulateTriangulatedIndicies()
    {
        //Generate indicies.
        _Indicies = model.GetTriangulatedModel().ToArray();
    }

    public override void OnModelUpdate(Model model, ModelUpdateType info, object data)
    {

    }

    public static IEnumerable<GLModelComponent> AllComponents(Hierarchy hierarchy)
    {
        foreach (Model model in hierarchy.Models)
        {
            if (!model.TryGetComponent<GLModelComponent>(out ModelComponent? component))//Add component it DNE!
            {
                component = model.AddComponent<GLModelComponent>(new GLModelComponent());
            }
            yield return (GLModelComponent)component!;
        }
    }
}
