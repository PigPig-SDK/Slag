using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace OpenglAvaloniaTest.ViewModels;

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Models;
using OpenTK.Mathematics;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;

public class GLControl : OpenGlControlBase
{
    public static GLControl? Instance;
    public static RenderMode RenderMode = RenderMode.Solid;
    /// <summary>
    /// This stack exists so that OpenGL Actions are all executed on the main thread
    /// </summary>
    public Stack<Action<GlInterface>> ModelActions = new();

    private ShaderProgram _triangleShaderProgram = new();
    private ShaderProgram _edgeShaderProgram = new();
    private ShaderProgram _vertexShaderProgram = new();

    private Camera _camera;

    private const string TriangleVertexShader = "Shaders/triangle.vs";
    private const string TriangleFragmentShader = "Shaders/triangle.fs";

    private const string EdgeVertexShader = "Shaders/edge.vs";
    private const string EdgeFragmentShader = "Shaders/edge.fs";

    private const string VertexVertexShader = "Shaders/vertex.vs";
    private const string VertexFragmentShader = "Shaders/vertex.fs";

    private List<Model> _LateModelAddition = [];

    public GLControl()
    {
        Instance = this;
        //Link camera
        _camera = new Camera(this);
        this.PointerPressed += _camera.OnMouseDown;
        this.PointerReleased += _camera.OnMouseUp;
        this.PointerMoved += _camera.OnPointerMove;
        this.AddHandler(PointerWheelChangedEvent, _camera.OnWheel, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    public void OnModelAdded(Model model) => _LateModelAddition.Add(model);

    public static void CheckError(GlInterface gl)
    {
        int err;
        while ((err = gl.GetError()) != GL_NO_ERROR)
            Console.WriteLine("OPENGL ERROR:" + err);
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        CheckError(gl);
        Console.WriteLine($"Renderer: {gl.GetString(GL_RENDERER)} Version: {gl.GetString(GL_VERSION)}");

        Console.WriteLine("Generating triangle shader program...");
        _triangleShaderProgram.GenerateShaderProgram(gl, TriangleVertexShader, TriangleFragmentShader);

        Console.WriteLine("Generating edge shader program...");
        _edgeShaderProgram.GenerateShaderProgram(gl, EdgeVertexShader, EdgeFragmentShader);

        Console.WriteLine("Generating vertex shader program...");
        _vertexShaderProgram.GenerateShaderProgram(gl, VertexVertexShader, VertexFragmentShader);

        CheckError(gl);

        gl.UseProgram(_triangleShaderProgram.ProgramID);

        //Add components and buffer data to opengl.
        foreach (Model model in SceneHierarchy.Instance.AllModels())
        {
            BindOpenglComponent(model, gl);
        }

        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
    }

    public bool BindOpenglComponent(Model model, GlInterface gl)
    {
        if (model.HasComponent(typeof(GLModelComponent))) return false;
        GLModelComponent? glComponent = model.AddComponent<GLModelComponent>(new GLModelComponent()) as GLModelComponent;
        
        if(glComponent == null) return false;
        glComponent.GenerateBuffers(gl);

        return true;
    }

    void CheckLateObjects(GlInterface gl)
    {
        if (_LateModelAddition.Count == 0) return;

        foreach (Model model in _LateModelAddition)
        {
            BindOpenglComponent(model, gl);
        }
        _LateModelAddition.Clear();
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        CheckLateObjects(gl);

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
        var scaling = (this.VisualRoot != null) ? this.VisualRoot!.RenderScaling : 1.0;
        gl.Viewport(0, 0, (int)(Bounds.Width * scaling), (int)(Bounds.Height * scaling));

        gl.ClearColor(0.1f, 0.1f, 0.1f, 0.85f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

        //Camera controls
        Matrix4 view = _camera.CreateLookAt();
        float aspect = (float)(Bounds.Width / (double)Bounds.Height);
        Matrix4 proj = _camera.CreatePrespective(aspect);

        //gl.LineWidth(10);

        CheckError(gl);

        //PRIMARY RENDERING!
        ExecuteGlStack(gl);
        RenderModels(gl, ref view, ref proj);
        RenderTools(gl, ref view, ref proj);

        RequestNextFrameRendering();
    }

    private void ExecuteGlStack(GlInterface gl)
    {
        while (ModelActions.Count > 0)
        {
            var action = ModelActions.Pop();
            action.Invoke(gl);
        }
    }

    private void RenderTools(GlInterface gl, ref Matrix4 view, ref Matrix4 proj)
    {
        gl.Disable(GL_DEPTH_TEST);
        foreach (GLModelComponent component in GLModelComponent.AllComponents(SceneHierarchy.Instance.SceneTools()))
        {
            if (component.model.Hidden) continue;

            Matrix4 modelTransformation = component.model.GetModelMatrix();
            _triangleShaderProgram.UseProgram(gl, modelTransformation, view, proj, _camera.Origin);
            component.RenderModel(gl);
        }
    }

    private void RenderModels(GlInterface gl, ref Matrix4 view, ref Matrix4 proj)
    {
        gl.Enable(GL_DEPTH_TEST);
        //Update models that are not 'fresh'

        foreach (GLModelComponent component in GLModelComponent.AllComponents(SceneHierarchy.Instance.SceneModels()))
        {
            if (component.model.Hidden) continue;

            Matrix4 modelTransformation = component.model.GetModelMatrix();

            component.SelectonMassUpdate(gl);

            //Triangles
            if (RenderMode.HasFlag(RenderMode.Triangles))
            {
                _triangleShaderProgram.UseProgram(gl, modelTransformation, view, proj, _camera.Origin);
                component.RenderModel(gl);
            }

            if (RenderMode.HasFlag(RenderMode.Edges))
            {
                _edgeShaderProgram.UseProgram(gl, modelTransformation, view, proj, _camera.Origin);
                component.RenderEdges(gl);
            }

            if (RenderMode.HasFlag(RenderMode.Verts))
            {
                _vertexShaderProgram.UseProgram(gl, modelTransformation, view, proj, _camera.Origin);
                component.RenderVerts(gl);
            }

            gl.DepthMask(1);//true
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        SceneHierarchy.Instance.OnModelAdded -= OnModelAdded;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        //Make the element clickable.
        context.FillRectangle(Brushes.Transparent, new Avalonia.Rect(Bounds.Size));
    }

}