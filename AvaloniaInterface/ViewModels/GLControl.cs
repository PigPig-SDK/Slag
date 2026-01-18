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
using System;
using System.IO;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;
using OpenTK.Mathematics;

public class GLControl : OpenGlControlBase
{
    private int _shaderProgram;

    private int _modelMatrixLoc, _projectionMatrixLoc, _viewMatrixLoc, _cameraLocationLoc;

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

    void GenerateShaders(GlInterface gl)
    {
        //Compile/link shaders
        int vertexShader = gl.CreateShader(GL_VERTEX_SHADER);
        Console.WriteLine("Vertex shader error : " + gl.CompileShaderAndGetError(vertexShader, LoadShaderFile(TriangleVertexShader)));

        int fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
        Console.WriteLine("Fragment shader error : " + gl.CompileShaderAndGetError(fragmentShader, LoadShaderFile(TriangleFragmentShader)));

        //Create shaderprogram
        _shaderProgram = gl.CreateProgram();
        gl.AttachShader(_shaderProgram, vertexShader);
        gl.AttachShader(_shaderProgram, fragmentShader);
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
    {
        CheckError(gl);
        Console.WriteLine($"Renderer: {gl.GetString(GL_RENDERER)} Version: {gl.GetString(GL_VERSION)}");

        GenerateShaders(gl);

        //Bind to string location with attribute
        Console.WriteLine("Link shader program: " + gl.LinkProgramAndGetError(_shaderProgram));

        CheckError(gl);
        //Bind uniforms
        _modelMatrixLoc = gl.GetUniformLocationString(_shaderProgram, "model_matrix");
        _projectionMatrixLoc = gl.GetUniformLocationString(_shaderProgram, "projection_matrix");
        _viewMatrixLoc = gl.GetUniformLocationString(_shaderProgram, "view_matrix");
        _cameraLocationLoc = gl.GetUniformLocationString(_shaderProgram, "camera_location");

        gl.UseProgram(_shaderProgram);
        gl.Enable(GL_DEPTH_TEST);

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
        
        gl.UniformMatrix4fv(_viewMatrixLoc, 1, false, &view);
        gl.UniformMatrix4fv(_projectionMatrixLoc, 1, false, &proj);
        Vector3 cameraLocation = _camera.Origin;
        gl.Uniform3f(_cameraLocationLoc, cameraLocation.X, cameraLocation.Y, cameraLocation.Z);

        //Draw all models
        gl.Enable(GL_DEPTH_TEST);
        foreach (GLModelComponent c in GLModelComponent.AllComponents(SceneHierarchy.Instance.SceneModels()))
        {
            c.RenderModel(gl, _modelMatrixLoc);
        }

        //Draw tools models
        gl.Disable(GL_DEPTH_TEST);
        foreach (GLModelComponent c in GLModelComponent.AllComponents(SceneHierarchy.Instance.SceneTools()))
        {
            c.RenderModel(gl, _modelMatrixLoc);
        }
        RequestNextFrameRendering();
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

    public static string LoadShaderFile(string shaderFile)
    {
        ArgumentNullException.ThrowIfNull(shaderFile);

        return File.ReadAllText(shaderFile);
    }
}