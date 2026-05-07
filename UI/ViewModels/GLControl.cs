using System.Collections.Generic;
namespace UI.ViewModels;

using Avalonia.Controls;
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
    private static GLControl? _instance;
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
    public static RenderMode RenderMode { get; set; } = RenderMode.Solid;
    /// <summary>
    /// This stack exists so that OpenGL Actions are all executed on the main thread
    /// </summary>
    public Queue<Action<GlInterface>> ModelActions { get; private set; } = new();

    private readonly ShaderProgram _triangleShaderProgram = new();
    private readonly ShaderProgram _edgeShaderProgram = new();
    private readonly ShaderProgram _vertexShaderProgram = new();
    private readonly ShaderProgram _depthShaderProgram = new();
    private readonly ShaderProgram _outlineShaderProgram = new();


    public float Aspect { get; private set; }

    private Camera _camera;

    private const string TriangleVertexShader = "Shaders/triangle.vs";
    private const string TriangleFragmentShader = "Shaders/triangle.fs";

    private const string EdgeVertexShader = "Shaders/edge.vs";
    private const string EdgeFragmentShader = "Shaders/edge.fs";

    private const string VertexVertexShader = "Shaders/vertex.vs";
    private const string VertexFragmentShader = "Shaders/vertex.fs";

    private const string DepthVertexShader = "Shaders/depth.vs";
    private const string DepthFragmentShader = "Shaders/depth.fs";

    private const string OutlineVertexShader = "Shaders/outline.vs";
    private const string OutlineFragmentShader = "Shaders/outline.fs";

    private readonly List<Model> _lateModelAddition = [];

    private readonly Dictionary<RenderMode, ShaderProgram> _renderModeToShaderProgram;

    private int? _shadowmapFrameBuffer;
    private int? _depthMap;

    private int? _defaultFrameBuffer;
    private int? _frameBufferTexture;
    private int? _depthStencilRbo;

    Matrix4 LightSpaceMatrix = Matrix4.Identity;
    private int _fboWidth;
    private int _fboHeight;

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
            { RenderMode.Depth, _depthShaderProgram},
            { RenderMode.Outline, _outlineShaderProgram} };
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
        _triangleShaderProgram.IsSelectionHidden = () => !SelectionManager.Instance.CurrentSelectionMode.HasFlag(SelectionMode.Face);

        _edgeShaderProgram.GenerateShaderProgram(gl, EdgeVertexShader, EdgeFragmentShader);
        _edgeShaderProgram.IsSelectionHidden = () => !SelectionManager.Instance.CurrentSelectionMode.HasFlag(SelectionMode.Edge);

        _vertexShaderProgram.GenerateShaderProgram(gl, VertexVertexShader, VertexFragmentShader);
        _vertexShaderProgram.IsSelectionHidden = () => !SelectionManager.Instance.CurrentSelectionMode.HasFlag(SelectionMode.Vertex);

        _depthShaderProgram.GenerateShaderProgram(gl, DepthVertexShader, DepthFragmentShader);
        _outlineShaderProgram.GenerateShaderProgram(gl, OutlineVertexShader, OutlineFragmentShader);

        CheckError(gl);

        gl.UseProgram(_triangleShaderProgram.ProgramID);

        GenerateShadowmapFrameBuffer(gl);

        GenerateDefaultFrameBuffer(gl);

        //Add components and buffer data to opengl.
        foreach (Model model in SceneHierarchy.Instance.GetModels(HierarchyType.All))
        {
            SelectionComponent.BindComponent(model);
            GLComponent.BindComponent(model, gl);
        }
        

        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
    }

    private void GenerateShadowmapFrameBuffer(GlInterface gl)
    {
        _shadowmapFrameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(GL_FRAMEBUFFER, _shadowmapFrameBuffer.Value);
        _depthMap = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, _depthMap.Value);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GlConstantsExtended.GL_DEPTH_COMPONENT24, SunControls.ShadowMapWidth, SunControls.ShadowMapHeight, 0, GlConsts.GL_DEPTH_COMPONENT, GlConstantsExtended.GL_UNSIGNED_INT, IntPtr.Zero);

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
            throw new InvalidOperationException($"Shadowmap framebuffer incomplete: {status}");
        }

    }

    private void SetScreenSize(GlInterface gl ,int width, int height)
    {
        if (_frameBufferTexture is null) return;

        gl.BindTexture(GL_TEXTURE_2D, _frameBufferTexture.Value);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GlConstantsExtended.GL_RGBA, width, height, 0, GlConsts.GL_DEPTH_COMPONENT, GlConstantsExtended.GL_UNSIGNED_INT, IntPtr.Zero);
    }
    private void GenerateDefaultFrameBuffer(GlInterface gl)
    {
        _defaultFrameBuffer = gl.GenFramebuffer();
        gl.BindFramebuffer(GL_FRAMEBUFFER, _defaultFrameBuffer.Value);

        // Color attachment — all three format args consistent
        _frameBufferTexture = gl.GenTexture();
        gl.BindTexture(GL_TEXTURE_2D, _frameBufferTexture.Value);
        gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 1024, 1024, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0,
                                GL_TEXTURE_2D, _frameBufferTexture.Value, 0);

        // Depth+stencil — same dimensions as color texture
        _depthStencilRbo = gl.GenRenderbuffer();
        gl.BindRenderbuffer(GL_RENDERBUFFER, _depthStencilRbo.Value);
        gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, 1024, 1024);
        gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GlConstantsExtended.GL_DEPTH_STENCIL_ATTACHMENT,
                                   GL_RENDERBUFFER, _depthStencilRbo.Value);

        var status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
        if (status != GL_FRAMEBUFFER_COMPLETE)
            throw new InvalidOperationException($"Default framebuffer incomplete: {status}");
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

        RenderWorld(gl, fb);

        RequestNextFrameRendering();
    }
    private void RenderShadowmap(GlInterface gl)
    {
        if(_shadowmapFrameBuffer == null)
            throw new InvalidOperationException($"{nameof(_shadowmapFrameBuffer)} is null while calling {nameof(RenderShadowmap)}!");

        //World rendering
        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _shadowmapFrameBuffer!.Value);
        gl.Viewport(0, 0, SunControls.ShadowMapWidth, SunControls.ShadowMapHeight);
        gl.ClearColor(0.0f,0.0f,0.0f,1.0f);
        gl.Clear(GlConsts.GL_DEPTH_BUFFER_BIT);

        //Camera controls
        Matrix4 view = Matrix4.LookAt(_camera.LookAt + (SunControls.SunAngle * SunControls.SunClippingDistance), _camera.LookAt, new Vector3(0, 1, 0));
        Aspect = (float)(SunControls.ShadowMapWidth / (double)SunControls.ShadowMapHeight);
        float near_plane = 0.1f, far_plane = 200.0f;
        Matrix4.CreateOrthographic(30.0f, 30.0f, near_plane, far_plane, out Matrix4 proj);
        LightSpaceMatrix = view * proj;

        gl.Enable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Model, ref view, ref proj, RenderMode.Depth);
    }

    void AdjustScreenSize(GlInterface gl, int width, int height)
    {
        if (width != _fboWidth || height != _fboHeight)
        {
            _fboWidth = width;
            _fboHeight = height;

            gl.BindTexture(GL_TEXTURE_2D, _frameBufferTexture!.Value);
            gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0,
                          GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);

            gl.BindRenderbuffer(GL_RENDERBUFFER, _depthStencilRbo!.Value);
            gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
        }
    }

    private void RenderWorld(GlInterface gl, int fb)
    {
        //World rendering
        var scaling = (this.VisualRoot != null) ? this.VisualRoot!.RenderScaling : 1.0;
        int width = (int)(Bounds.Width * scaling);
        int height = (int)(Bounds.Height * scaling);

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _defaultFrameBuffer!.Value);
        gl.ColorMask(true, true, true, true);
        gl.Clear(GL_STENCIL_BUFFER_BIT);
        AdjustScreenSize(gl, width, height);
        gl.Viewport(0, 0, width, height);
        gl.ClearColor(0.1f, 0.1f, 0.1f, 0.85f);
        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);
        
        //Camera controls
        Matrix4 view = _camera.CreateLookAt();
        Aspect = (float)(Bounds.Width / (double)Bounds.Height);
        Matrix4 proj = _camera.CreatePrespective(Aspect);

        //Step 1 : render world normally.
        gl.Enable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Model, ref view, ref proj);
        RenderModels(gl, HierarchyType.EditVisualizer, ref view, ref proj, RenderMode.Triangles);

        //Step 2 : Render outlines on selected objects using the stencil buffer.
        gl.Disable(GL_DEPTH_TEST);
        gl.ColorMask(false, false, false, false);
        gl.Enable(GlConstantsExtended.GL_STENCIL_TEST);
        gl.StencilMask(0xFF);//Write only
        gl.StencilFunc(GlConstantsExtended.GL_ALWAYS, 1, 0xFF);
        gl.StencilOp(GlConstantsExtended.GL_KEEP, GlConstantsExtended.GL_KEEP, GlConstantsExtended.GL_REPLACE);
        RenderModels(gl, HierarchyType.Model | HierarchyType.Selected, ref view, ref proj);

        // !! Draw outlines using the stencil buffer !!
        gl.ColorMask(true, true, true, true);
        gl.StencilMask(0x00);//Read only
        gl.StencilFunc(GlConstantsExtended.GL_NOTEQUAL, 1, 0xFF);
        RenderModels(gl, HierarchyType.Model | HierarchyType.Selected, ref view, ref proj, RenderMode.Outline);

        //Restore
        gl.StencilMask(0xFF);
        gl.Disable(GlConstantsExtended.GL_STENCIL_TEST);

        gl.Disable(GL_DEPTH_TEST);
        RenderModels(gl, HierarchyType.Tool, ref view, ref proj, RenderMode.Triangles);

        gl.BindFramebuffer(GL_READ_FRAMEBUFFER, _defaultFrameBuffer!.Value);
        gl.BindFramebuffer(GL_DRAW_FRAMEBUFFER, fb);
        gl.BlitFramebuffer(
            0, 0, width, height,
            0, 0, width, height,
            GL_COLOR_BUFFER_BIT, GL_NEAREST
        );

        gl.BindFramebuffer(GL_FRAMEBUFFER, fb);
    }

    /// <summary>
    /// This function executes a set of instrctions before drawing the next frame.
    /// This may include model edits/deletions/additions/ect.
    /// </summary>
    private void ExecuteGlStack(GlInterface gl)
    {
        while (ModelActions.Count > 0)
        {
            var action = ModelActions.Dequeue();
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
                activeShader.UseProgram(gl, view, proj, _camera.Origin, LightSpaceMatrix, _depthMap!.Value, SunControls.SunAngle);

                foreach (IRenderObject renderObject in AllRenderables(SceneHierarchy.Instance.GetModels(hierarchy)))
                {
                    if (renderObject.Hidden) continue;
                    if (!renderObject.Selected && hierarchy.HasFlag(HierarchyType.Selected)) continue;
                    Matrix4 modelTransformation = renderObject.ModelMatrix;
                    gl.UniformMatrix4fv(activeShader.ModelMatrixLocation, 1, false, (float*)&modelTransformation);

                    switch(mode) 
                    {
                        default:
                            {
                                renderObject.RenderModel(gl, activeShader);
                                break;
                            }
                        case RenderMode.Edges:
                            {
                                renderObject.RenderEdges(gl);
                                break;
                            }
                        case RenderMode.Verts:
                            {
                                renderObject.RenderVertices(gl);
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