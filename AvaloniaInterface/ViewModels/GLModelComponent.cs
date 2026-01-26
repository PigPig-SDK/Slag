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
    private int? _SelectionBuffer = null;

    private int? _TriangleArrayObject;
    private int? _EdgeArrayObject;

    private int _IndiciesCount = 0;
    private int _VertexCount = 0;
    private int _EdgeIndiciesCount = 0;

    private GlInterface? glInterface = null;

    private Dictionary<uint, List<uint>> _SharpIndicies = [];

    public override void OnAddedToModel(Model model)
    {
        ModelSelection? modelSelection = model.GetComponent<ModelSelection>();
        modelSelection!.OnSelectionChanged += OnSelectionChanged;
        modelSelection!.OnSelectionMassUpdate += OnSelectionMassUpdate;
    }

    public unsafe void SelectonMassUpdate(GlInterface? gl)
    {
        if (gl == null)
        {
            Console.WriteLine("Tried to update selection buffer when opengl interface is null!");
            return;
        }

        //Generate array.
        byte[] selectionData = new byte[_VertexCount];
        for (int i = 0; i < selectionData.Length; i++)
        {
            selectionData[i] = model.GetComponent<ModelSelection>()!.IsVertexSelected((uint)i) ? (byte)1 : (byte)0;
        }

        gl.BindBuffer(GL_ARRAY_BUFFER, _SelectionBuffer!.Value);
        fixed (byte* ptr = selectionData)
        {
            gl!.BufferSubData(GL_ARRAY_BUFFER, (nint)0, (nint)(_VertexCount * sizeof(byte)), (IntPtr)ptr);
        }
    }

    private unsafe void OnSelectionMassUpdate()
    {

    }

    private void OnSelectionChanged(uint index, bool isSelected)
    {

    }

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

        //Setup Buffers
        _TriangleArrayObject = gl.GenVertexArray();
        _VertexBufferObject = gl.GenBuffer();
        _IndiciesBuffer = gl.GenBuffer();
        _SelectionBuffer = gl.GenBuffer();
        _EdgeArrayObject = gl.GenVertexArray();
        _EdgeIndiciesBuffer = gl.GenBuffer();

        //TRIANGLE
        gl.BindVertexArray(_TriangleArrayObject!.Value);
        InitializeTrangleBuffers(gl);
        SetLocationsInsideVAO(gl);

        //EDGE
        gl.BindVertexArray(_EdgeArrayObject!.Value);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _EdgeIndiciesBuffer!.Value);
        SetLocationsInsideVAO(gl);
        UpdateEdgeBuffers(gl);
    }

    private void SetLocationsInsideVAO(GlInterface gl)
    {
        //Vertex Data
        gl.BindBuffer(GL_ARRAY_BUFFER, _VertexBufferObject!.Value);

        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, Vertex.GetSize(), 0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("Normal"));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 2, GL_FLOAT, 0, Vertex.GetSize(), Marshal.OffsetOf<Vertex>("UV"));
        gl.EnableVertexAttribArray(2);

        //Selection Related data
        gl.BindBuffer(GL_ARRAY_BUFFER, _SelectionBuffer!.Value);
        gl.VertexAttribPointer(3, 1, GL_UNSIGNED_BYTE, 0, sizeof(byte), 0);
        gl.EnableVertexAttribArray(3);
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

    public unsafe void InitializeTrangleBuffers(GlInterface gl)
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

        //Buffer selection
        byte[] selectionData = new byte[verts.Length];
        for (int i = 0; i < selectionData.Length; i++)
        {
            selectionData[i] = i % 10 == 0 ? (byte)1 : (byte)0;//Temporary selection data
        }

        gl.BindBuffer(GL_ARRAY_BUFFER, _SelectionBuffer!.Value);
        fixed (byte* ptr = selectionData)
        {
            gl.BufferData(GL_ARRAY_BUFFER, sizeof(byte) * verts.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        Console.WriteLine($"{nameof(selectionData)} Upload Error: {gl.GetError()}");
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
        if (_TriangleArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_TriangleArrayObject)} while its null");
        gl.BindVertexArray(_TriangleArrayObject!.Value);
        gl.DrawElements(GL_TRIANGLES, _IndiciesCount, GL_UNSIGNED_INT, 0);
    }

    internal void RenderEdges(GlInterface gl)
    {
        if (_EdgeArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_EdgeArrayObject)} while its null");
        gl.BindVertexArray(_EdgeArrayObject!.Value);
        gl.DrawElements(GL_LINES, _EdgeIndiciesCount, GL_UNSIGNED_INT, 0);
        gl.BindVertexArray(0);
    }

    internal void RenderVerts(GlInterface gl)
    {
        if (_TriangleArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_TriangleArrayObject)} while its null");
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
        if(_SelectionBuffer != null) glInterface.DeleteBuffer(_SelectionBuffer!.Value);

        _SelectionBuffer = null;
        _TriangleArrayObject = null;
        _IndiciesBuffer = null;
        _VertexBufferObject = null;

        glInterface = null;
    }
}
