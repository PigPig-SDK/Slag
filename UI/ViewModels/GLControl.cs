using System.Collections.Generic;
namespace UI.ViewModels;

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using static Avalonia.OpenGL.GlConsts;

public class GLControl : OpenGlControlBase
{
    public static GLControl? _instance;
    public static GLControl Instance
    {
        get
        {
            if (_instance == null) throw new InvalidOperationException($"No instance of {nameof(GLControl)} exists!");
            return _instance;
        }
        private set
        {
            if (_instance != null) throw new InvalidOperationException($"An instance of {nameof(GLControl)} already exists!");
            _instance = value;
        }
    }
    public static RenderMode RenderMode = RenderMode.Solid;
    /// <summary>
    /// This stack exists so that OpenGL Actions are all executed on the main thread
    /// </summary>
    public Stack<Action<GlInterface>> ModelActions = new();

    private readonly ShaderProgram _triangleShaderProgram = new();
    private readonly ShaderProgram _edgeShaderProgram = new();
    private readonly ShaderProgram _vertexShaderProgram = new();
    private readonly ShaderProgram _depthShaderProgram = new();


    public float Aspect { get; private set; } = 0;

    private Camera _camera;

    private const string TriangleVertexShader = "Shaders/triangle.vs";
    private const string TriangleFragmentShader = "Shaders/triangle.fs";

    private const string EdgeVertexShader = "Shaders/edge.vs";
    private const string EdgeFragmentShader = "Shaders/edge.fs";

    private const string VertexVertexShader = "Shaders/vertex.vs";
    private const string VertexFragmentShader = "Shaders/vertex.fs";

    private const string DepthVertexShader = "Shaders/depth.vs";
    private const string DepthFragmentShader = "Shaders/depth.fs";

    private readonly List<Model> _lateModelAddition = [];

    private readonly Dictionary<RenderMode, ShaderProgram> _renderModeToShaderProgram;

    private int? _shadowmapFrameBuffer = null;
    private int? _depthMap = null;
    Matrix4 LightSpaceMatrix = Matrix4.Identity;
    public Vector3 SunAngle = new Vector3(50, 50, 25).Normalized();

    private const int _shadowWidth = 2048, _shadowHeight = 2048;
    private const float _sunDistance = 75;

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
        
        _renderModeToShaderProgram = new() {
            { RenderMode.Triangles, _triangleShaderProgram },
            { RenderMode.Edges, _edgeShaderProgram },
            { RenderMode.Verts, _vertexShaderProgram},
            { RenderMode.Depth, _depthShaderProgram}};
    }

    private void OnPress(object? sender, PointerPressedEventArgs e) => Focus();

    public void OnModelAdded(HierarchyType hierarchyType, Model model) => _lateModelAddition.Add(model);

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

        _triangleShaderProgram.GenerateShaderProgram(gl, TriangleVertexShader, TriangleFragmentShader);

        _edgeShaderProgram.GenerateShaderProgram(gl, EdgeVertexShader, EdgeFragmentShader);

        _vertexShaderProgram.GenerateShaderProgram(gl, VertexVertexShader, VertexFragmentShader);

        _depthShaderProgram.GenerateShaderProgram(gl, DepthVertexShader, DepthFragmentShader);

        CheckError(gl);

        gl.UseProgram(_triangleShaderProgram.ProgramID);

        //Generate frame buffers
        _shadowmapFrameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(GL_FRAMEBUFFER, _shadowmapFrameBuffer.Value);
        _depthMap = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, _depthMap.Value);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GlConstantsExtended.GL_DEPTH_COMPONENT24, _shadowWidth, _shadowHeight, 0, GlConsts.GL_DEPTH_COMPONENT, GlConstantsExtended.GL_UNSIGNED_INT, IntPtr.Zero);

        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        gl.TexParameteri(GL_TEXTURE_2D, GlConstantsExtended.GL_TEXTURE_WRAP_S, GlConstantsExtended.GL_CLAMP_TO_EDGE);
        gl.TexParameteri(GL_TEXTURE_2D, GlConstantsExtended.GL_TEXTURE_WRAP_T, GlConstantsExtended.GL_CLAMP_TO_EDGE);
        gl.TexParameteri(GL_TEXTURE_2D, GlConstantsExtended.GL_TEXTURE_COMPARE_MODE, GlConstantsExtended.GL_NONE);

        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, _depthMap.Value, 0);
        gl.DrawBuffers(0, [GlConstantsExtended.GL_NONE]);
        gl.ReadBuffer(GlConstantsExtended.GL_NONE);

        var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
        if (gl.CheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
        {
            throw new Exception($"Shadowmap framebuffer incomplete: {status}");
        }

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
        if (_lateModelAddition.Count == 0) return;

        foreach (Model model in _lateModelAddition)
        {
            SelectionComponent.BindComponent(model);
            GLComponent.BindComponent(model, gl);
            //TODO: Implement BVH if optimization is truly warranted.
            //BVHComponent.BindComponent(model);
        }
        _lateModelAddition.Clear();
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int fb)
    {
        CheckAppendingModels(gl);

        ExecuteGlStack(gl);

        RenderShadowmap(gl);

        RenderWorld(gl,fb);

        RequestNextFrameRendering();
    }
    private void RenderShadowmap(GlInterface gl)
    {
        if(_shadowmapFrameBuffer == null)
            throw new InvalidOperationException($"{nameof(_shadowmapFrameBuffer)} is null while calling {nameof(RenderShadowmap)}!");

        //World rendering
        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _shadowmapFrameBuffer!.Value);
        gl.Viewport(0, 0, _shadowWidth, _shadowHeight);
        gl.ClearColor(0.0f,0.0f,0.0f,1.0f);
        gl.Clear(GlConsts.GL_DEPTH_BUFFER_BIT);

        //Camera controls
        Matrix4 view = Matrix4.LookAt(_camera.LookAt + (SunAngle * _sunDistance), _camera.LookAt, new Vector3(0, 1, 0));
        Aspect = (float)(_shadowWidth / (double)_shadowHeight);
        float near_plane = 0.1f, far_plane = 200.0f;
        Matrix4.CreateOrthographic(30.0f, 30.0f, near_plane, far_plane, out Matrix4 proj);
        LightSpaceMatrix = view * proj;

        gl.Enable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Model, ref view, ref proj, RenderMode.Depth);
    }

    private void RenderWorld(GlInterface gl, int fb)
    {
        //World rendering
        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, fb);
        var scaling = (this.VisualRoot != null) ? this.VisualRoot!.RenderScaling : 1.0;
        gl.Viewport(0, 0, (int)(Bounds.Width * scaling), (int)(Bounds.Height * scaling));

        gl.ClearColor(0.1f, 0.1f, 0.1f, 0.85f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);

        //Camera controls
        Matrix4 view = _camera.CreateLookAt();
        Aspect = (float)(Bounds.Width / (double)Bounds.Height);
        Matrix4 proj = _camera.CreatePrespective(Aspect);

        gl.Enable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Model, ref view, ref proj);
        RenderModels(gl, HierarchyType.EditVisualizer, ref view, ref proj, RenderMode.Triangles);

        gl.Disable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Tool, ref view, ref proj, RenderMode.Triangles);
        RenderUI();
        RenderOverview(gl);
    }

    private void RenderOverview(GlInterface gl)
    {
        
    }

    private void RenderUI()
    {

    }

    /// <summary>
    /// This function executes a set of instrctions before drawing the next frame.
    /// This may include model edits/deletions/additions/ect.
    /// </summary>
    private void ExecuteGlStack(GlInterface gl)
    {
        while (ModelActions.Count > 0)
        {
            var action = ModelActions.Pop();
            action.Invoke(gl);
        }
    }

    private unsafe void RenderModels(GlInterface gl, HierarchyType hierarchy, ref Matrix4 view, ref Matrix4 proj, RenderMode? rendermode = null)
    {
        if (rendermode == null) rendermode = RenderMode;
        
        foreach (RenderMode mode in _renderModeToShaderProgram.Keys)
        {
            if(rendermode.Value.HasFlag(mode))
            {
                ShaderProgram activeShader = _renderModeToShaderProgram[mode];
                activeShader.UseProgram(gl, view, proj, _camera.Origin, LightSpaceMatrix, _depthMap!.Value, SunAngle);

                foreach (IRenderObject component in AllRenderables(SceneHierarchy.Instance.GetModels(hierarchy)))
                {
                    if (component.Hidden) continue;
                    Matrix4 modelTransformation = component.ModelMatrix;
                    gl.UniformMatrix4fv(activeShader.ModelMatrixLocation, 1, false, (float*)&modelTransformation);

                    switch(mode) 
                    {
                        default:
                            {
                                component.RenderModel(gl, activeShader);
                                break;
                            }
                        case RenderMode.Edges:
                            {

                                component.RenderEdges(gl);
                                break;
                            }
                        case RenderMode.Verts:
                            {
                                component.RenderVertices(gl);
                                break;
                            }
                    }
                }
            }
        }
    }

    public static IEnumerable<IRenderObject> AllRenderables(IEnumerable<Model> models)
    {
        foreach (Model model in models)
        {
            foreach(IRenderObject obj in model.GetAllOfType<IRenderObject>())
            {
                yield return obj;
            }
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        SceneHierarchy.Instance.OnModelAdded -= OnModelAdded;

        if(_depthMap.HasValue) gl.DeleteTexture(_depthMap.Value);

        if(_shadowmapFrameBuffer.HasValue) gl.DeleteFramebuffer(_shadowmapFrameBuffer.Value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        //Make the element clickable.
        context.FillRectangle(Brushes.Transparent, new Avalonia.Rect(Bounds.Size));
    }

}