using Avalonia.OpenGL;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;
using static UI.ViewModels.GlConstantsExtended;

namespace UI.ViewModels;

public class GLComponent : ModelComponent, IRenderObject
{
    private int? _vertexBufferObject;
    private int? _indiciesBuffer;
    private int? _edgeIndiciesBuffer;
    private int? _selectionBuffer;

    private int? _triangleArrayObject;
    private int? _edgeArrayObject;

    private int _indiciesCount;
    private int _vertexCount;
    private int _edgeIndiciesCount;

    private GlInterface? glInterface;

    //Used for tools and such...
    public Color4? color;
    public bool IsFullbright;
    public bool UseTilemapRendering;

    public bool Hidden { get => Model.Hidden; set => Model.Hidden = value; }

    public Matrix4 ModelMatrix { get => Model.GetModelMatrix(); }

    public static event Action<GLComponent>? OnBoundToModel;

    public override void OnAddedToModel(Model model)
    {
        SelectionComponent? modelSelection = model.GetComponent<SelectionComponent>();
        modelSelection!.OnSelectionChanged += OnSelectionChanged;
        modelSelection!.OnSelectionMassUpdate += OnSelectionMassUpdate;
    }

    public unsafe void SelectonMassUpdate(GlInterface? gl)
    {
        if (gl == null)
        {
            return;
        }

        HashSet<uint> selectedVerts = [.. Model.GetComponent<SelectionComponent>()!.GetSelection<uint>()];

        //Generate array.
        byte[] selectionData = new byte[_vertexCount];
        for (uint i = 0; i < selectionData.Length; i++)
        {
            selectionData[i] = selectedVerts.Contains(i) ? (byte)1 : (byte)0;
        }

        gl.BindBuffer(GL_ARRAY_BUFFER, _selectionBuffer!.Value);
        fixed (byte* ptr = selectionData)
        {
            gl!.BufferSubData(GL_ARRAY_BUFFER, (nint)0, (nint)(_vertexCount * sizeof(byte)), (IntPtr)ptr);
        }
    }

    public unsafe void UpdateModel(GlInterface? gl)
    {
        if (gl == null)
        {
            return;
        }

        Vertex[] verts = Model.GetVertexBackingField();
        _vertexCount = Model.Verticies.Count;
        ComputeNormals(verts, Model.Indicies);

        gl.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject!.Value);
        fixed (Vertex* ptr = verts)
        {
            gl.BufferSubData(GL_ARRAY_BUFFER, (nint)0, Vertex.Size * _vertexCount, (IntPtr)ptr);
        }
    }

    private unsafe void OnSelectionMassUpdate(UpdateType update)
    {
        if (update.HasFlag(UpdateType.Ignore)) return;

        GLControl.Instance.ModelActions.Push(SelectonMassUpdate);
    }

    private void OnSelectionChanged(bool isSelected, UpdateType update)
    {
        if (update.HasFlag(UpdateType.Ignore)) return;

        OnSelectionMassUpdate(update);
    }

    public void OpenglRestart(GlInterface gl)
    {
        //Clear buffers
        _indiciesBuffer = null;
        _vertexBufferObject = null;
        GenerateBuffers(gl);
    }

    public void GenerateBuffers(GlInterface gl)
    {
        glInterface = gl;

        //Setup Buffers
        _triangleArrayObject = gl.GenVertexArray();
        _vertexBufferObject = gl.GenBuffer();
        _indiciesBuffer = gl.GenBuffer();
        _selectionBuffer = gl.GenBuffer();
        _edgeArrayObject = gl.GenVertexArray();
        _edgeIndiciesBuffer = gl.GenBuffer();

        //TRIANGLE
        gl.BindVertexArray(_triangleArrayObject!.Value);
        InitializeTrangleBuffers(gl);
        SetLocationsInsideVAO(gl);

        //EDGE
        gl.BindVertexArray(_edgeArrayObject!.Value);
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _edgeIndiciesBuffer!.Value);
        SetLocationsInsideVAO(gl);
        UpdateEdgeBuffers(gl);
    }

    private void SetLocationsInsideVAO(GlInterface gl)
    {
        //Vertex Data
        gl.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject!.Value);

        gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, Vertex.Size, 0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, Vertex.Size, Marshal.OffsetOf<Vertex>("Normal"));
        gl.EnableVertexAttribArray(1);

        gl.VertexAttribPointer(2, 2, GL_FLOAT, 0, Vertex.Size, Marshal.OffsetOf<Vertex>("UV"));
        gl.EnableVertexAttribArray(2);

        //Selection Related data
        gl.BindBuffer(GL_ARRAY_BUFFER, _selectionBuffer!.Value);
        gl.VertexAttribPointer(3, 1, GL_UNSIGNED_BYTE, 0, sizeof(byte), 0);
        gl.EnableVertexAttribArray(3);
    }

    public unsafe void UpdateEdgeBuffers(GlInterface gl)
    {
        gl.BindVertexArray(_edgeArrayObject!.Value);
        uint[] edgeIndicies = Model.GetEdgeIndicies();

        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _edgeIndiciesBuffer!.Value);
        fixed (uint* ptr = edgeIndicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * edgeIndicies.Length, (nint)ptr, GL_STATIC_DRAW);
        }
        _edgeIndiciesCount = edgeIndicies.Length;
    }

    public unsafe void InitializeTrangleBuffers(GlInterface gl)
    {
        gl.BindVertexArray(_triangleArrayObject!.Value);

        Vertex[] verts = [];
        List<uint> indiciesList = []; //List that will encounter lots of modification

        Model.GenerateTriangulatedModel(ref verts, ref indiciesList);

        //Recalculates the model to account for 'sharp' edges
        //ComputeSmoothing(ref indiciesList);

        uint[] indicies = indiciesList.ToArray();

        //Manage edges
        ComputeNormals(verts, indicies);
        _indiciesCount = indicies.Length;
        _vertexCount = Model.Verticies.Count;

        //Inform of vert data
        gl.BindBuffer(GL_ARRAY_BUFFER, _vertexBufferObject!.Value);
        fixed (Vertex* ptr = verts)
        {
            gl.BufferData(GL_ARRAY_BUFFER, Vertex.Size * _vertexCount, (nint)ptr, GL_STATIC_DRAW);
        }

        //Inform of indicies
        gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, _indiciesBuffer!.Value);
        fixed (uint* ptr = indicies)
        {
            gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * indicies.Length, (nint)ptr, GL_STATIC_DRAW);
        }

        //Buffer selection
        byte[] selectionData = new byte[verts.Length];
        gl.BindBuffer(GL_ARRAY_BUFFER, _selectionBuffer!.Value);
        fixed (byte* ptr = selectionData)
        {
            gl.BufferData(GL_ARRAY_BUFFER, sizeof(byte) * verts.Length, (nint)ptr, GL_STATIC_DRAW);
        }
    }

    public static bool BindComponent(Model model, GlInterface gl)
    {
        if (!model.HasComponent(typeof(GLComponent)))
        {
            GLComponent? glComponent = model.AddComponent<GLComponent>(new GLComponent()) as GLComponent;
            if (glComponent == null) return false;
            glComponent.GenerateBuffers(gl);
            OnBoundToModel?.Invoke(glComponent);
        }
        
        return true;
    }

    public void RenderModel(GlInterface gl, ShaderProgram program)
    {
        if (_triangleArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_triangleArrayObject)} while its null");
        

        program.SetColorUniform(gl, program.GetUniformLocation(gl, "color"), color ?? new Color4(1, 1, 1, 1));
        gl.Uniform1i(program.GetUniformLocation(gl, "useColor"), (color is null)? 0 : 1);
        gl.Uniform1i(program.GetUniformLocation(gl, "isFullbright"), (IsFullbright) ? 1 : 0);
        gl.Uniform1i(program.GetUniformLocation(gl, "useTilemap"), (UseTilemapRendering) ? 1 : 0);

        gl.BindVertexArray(_triangleArrayObject!.Value);
        gl.DrawElements(GL_TRIANGLES, _indiciesCount, GL_UNSIGNED_INT, 0);
    }

    public void RenderEdges(GlInterface gl)
    {
        if (_edgeArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_edgeArrayObject)} while its null");
        gl.BindVertexArray(_edgeArrayObject!.Value);
        //TODO: Find alternative to GL_LINES for better edge rendering.
        //For now, this will do.
        gl.DrawElements(GL_LINES, _edgeIndiciesCount, GL_UNSIGNED_INT, 0);
        gl.BindVertexArray(0);
    }

    public void RenderVertices(GlInterface gl)
    {
        if (_triangleArrayObject == null) throw new InvalidOperationException($"Tried to render {nameof(_triangleArrayObject)} while its null");
        gl.BindVertexArray(_triangleArrayObject!.Value);
        gl.DrawArrays(GL_POINTS, 0, _vertexCount);
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

    public override void OnModelUpdate(Model model, UpdateType info)
    {
        if((info & (UpdateType.Locational)) != 0)
        {
            GLControl.Instance.ModelActions.Push(UpdateModel);
        }

        //Redraw requested.
        if((info & UpdateType.Membership) != 0)
        {
            GLControl.Instance.ModelActions.Push(InitializeTrangleBuffers);
            GLControl.Instance.ModelActions.Push(UpdateEdgeBuffers);
        }
    }

    public static IEnumerable<GLComponent> AllComponents(IEnumerable<Model> models)
    {
        foreach (Model model in models)
        {
            if (!model.TryGetComponent<GLComponent>(out ModelComponent? component))//Add component it DNE!
            {
                //component = model.AddComponent<GLModelComponent>(new GLModelComponent());
                continue;
            }
            yield return (GLComponent)component!;
        }
    }

    public override void Dispose()
    {
        //Clean up the opengl resources.
        if (glInterface == null) return;

        UnloadBuffers(glInterface);

        _selectionBuffer = null;
        _triangleArrayObject = null;
        _indiciesBuffer = null;
        _vertexBufferObject = null;
        glInterface = null;
    }

    public void UnloadBuffers(GlInterface gl)
    {
        if (_vertexBufferObject != null) gl.DeleteBuffer(_vertexBufferObject!.Value);
        if (_indiciesBuffer != null) gl.DeleteBuffer(_indiciesBuffer!.Value);
        if (_triangleArrayObject != null) gl.DeleteVertexArray(_triangleArrayObject!.Value);
        if (_selectionBuffer != null) gl.DeleteBuffer(_selectionBuffer!.Value);
    }
}
