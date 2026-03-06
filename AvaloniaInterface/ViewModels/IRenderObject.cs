using Avalonia.OpenGL;
using OpenTK.Mathematics;

namespace OpenglAvaloniaTest.ViewModels;

public interface IRenderObject
{
    /// <summary>
    /// If the model should be rendered or not
    /// </summary>
    public bool Hidden { get; set; }
    /// The objects model matrix
    /// </summary>
    public Matrix4 ModelMatrix { get; }
    /// <summary>
    /// The function to generate buffers
    /// </summary>
    public unsafe void GenerateBuffers(GlInterface gl);
    /// <summary>
    /// The function call to render the model
    /// </summary>
    public void RenderModel(GlInterface gl);
    /// <summary>
    /// The function call to render the models edges
    /// </summary>
    public void RenderEdges(GlInterface gl);
    /// <summary>
    /// The function call to render the models verts
    /// </summary>
    public void RenderVertices(GlInterface gl);
    /// <summary>
    /// Used to cleanup buffers
    /// </summary>
    public void UnloadBuffers(GlInterface gl);
}
