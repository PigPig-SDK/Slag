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

    public float Aspect { get; private set; } = 0;

    private Camera _camera;

    private const string TriangleVertexShader = "Shaders/triangle.vs";
    private const string TriangleFragmentShader = "Shaders/triangle.fs";

    private const string EdgeVertexShader = "Shaders/edge.vs";
    private const string EdgeFragmentShader = "Shaders/edge.fs";

    private const string VertexVertexShader = "Shaders/vertex.vs";
    private const string VertexFragmentShader = "Shaders/vertex.fs";

    private List<Model> _LateModelAddition = [];

    Dictionary<RenderMode, ShaderProgram> renderModeToShaderProgram;

    public GLControl()
    {
        Instance = this;
        //Link camera
        _camera = new Camera(this);
        Focusable = true;

        PointerPressed += OnPress;
        PointerPressed += InputManager.Singleton.OnMouseDown;
        PointerReleased += InputManager.Singleton.OnMouseUp;
        PointerMoved += InputManager.Singleton.OnPointerMove;
        KeyDown += InputManager.Singleton.OnKeyDown;
        KeyUp += InputManager.Singleton.OnKeyUp;
        
        AddHandler(PointerWheelChangedEvent, _camera.OnWheel, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        
        renderModeToShaderProgram = new() {
            { RenderMode.Triangles, _triangleShaderProgram },
            { RenderMode.Edges, _edgeShaderProgram },
            { RenderMode.Verts, _vertexShaderProgram} };
    }

    private void OnPress(object? sender, PointerPressedEventArgs e) => Focus();

    public void OnModelAdded(HierarchyType hierarchyType, Model model) => _LateModelAddition.Add(model);

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
        foreach (Model model in SceneHierarchy.Instance.GetModels(HierarchyType.All))
        {
            SelectionComponent.BindComponent(model);
            GLComponent.BindComponent(model, gl);
        }

        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
    }

    void CheckAppendingModels(GlInterface gl)
    {
        if (_LateModelAddition.Count == 0) return;

        foreach (Model model in _LateModelAddition)
        {
            SelectionComponent.BindComponent(model);
            GLComponent.BindComponent(model, gl);
            BVHComponent.BindComponent(model);
        }
        _LateModelAddition.Clear();
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        CheckAppendingModels(gl);

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
        var scaling = (this.VisualRoot != null) ? this.VisualRoot!.RenderScaling : 1.0;
        gl.Viewport(0, 0, (int)(Bounds.Width * scaling), (int)(Bounds.Height * scaling));

        gl.ClearColor(0.1f, 0.1f, 0.1f, 0.85f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

        //Camera controls
        Matrix4 view = _camera.CreateLookAt();
        Aspect = (float)(Bounds.Width / (double)Bounds.Height);
        Matrix4 proj = _camera.CreatePrespective(Aspect);

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

    private unsafe void RenderTools(GlInterface gl, ref Matrix4 view, ref Matrix4 proj)
    {
        gl.Disable(GL_DEPTH_TEST);
        foreach (GLComponent component in GLComponent.AllComponents(SceneHierarchy.Instance.GetModels(HierarchyType.Tool)))
        {
            if (component.Model.Hidden) continue;

            Matrix4 modelTransformation = component.Model.GetModelMatrix();
            _triangleShaderProgram.UseProgram(gl, view, proj, _camera.Origin);
            gl.UniformMatrix4fv(_triangleShaderProgram.ModelMatrixLocation, 1, false, (float*)&modelTransformation);
            
            component.RenderModel(gl);
        }
    }


    private unsafe void RenderModels(GlInterface gl, ref Matrix4 view, ref Matrix4 proj)
    {
        gl.Enable(GL_DEPTH_TEST);
        
        foreach (RenderMode renderMode in renderModeToShaderProgram.Keys)
        {
            if(RenderMode.HasFlag(renderMode))
            {
                ShaderProgram activeShader = renderModeToShaderProgram[renderMode];
                activeShader.UseProgram(gl, view, proj, _camera.Origin);
                foreach (GLComponent component in GLComponent.AllComponents(SceneHierarchy.Instance.GetModels(HierarchyType.Model)))
                {
                    if (component.Model.Hidden) continue;
                    Matrix4 modelTransformation = component.Model.GetModelMatrix();
                    gl.UniformMatrix4fv(activeShader.ModelMatrixLocation, 1, false, (float*)&modelTransformation);

                    switch(renderMode)
                    {
                        case RenderMode.Triangles:
                            {
                                component.RenderModel(gl);
                                break;
                            }
                        case RenderMode.Edges:
                            {

                                component.RenderEdges(gl);
                                break;
                            }
                        case RenderMode.Verts:
                            {
                                component.RenderVerts(gl);
                                break;
                            }
                    }
                }
            }
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