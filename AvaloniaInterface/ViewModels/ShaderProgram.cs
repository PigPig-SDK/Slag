using Avalonia.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.OpenGL.GlConsts;

namespace OpenglAvaloniaTest.ViewModels;

public class ShaderProgram
{
    public int ProgramID;

    public int ModelMatrixLocation;

    private int _projectionMatrixLoc, _viewMatrixLoc, _cameraLocationLoc, _envMatrix;

    public void GenerateShaderProgram(GlInterface gl, string vertexShader, string fragmentShader)
    {
        int vertexShaderID = gl.CreateShader(GL_VERTEX_SHADER);
        string? error = gl.CompileShaderAndGetError(vertexShaderID, LoadShaderFile(vertexShader));
        if (error != null) Console.WriteLine("Vertex shader error : " + error);

        int fragmentShaderID = gl.CreateShader(GL_FRAGMENT_SHADER);
        error = gl.CompileShaderAndGetError(fragmentShaderID, LoadShaderFile(fragmentShader));
        if (error != null) Console.WriteLine("Fragment shader error : " + error);

        ProgramID = gl.CreateProgram();
        gl.AttachShader(ProgramID, vertexShaderID);
        gl.AttachShader(ProgramID, fragmentShaderID);

        error = gl.LinkProgramAndGetError(ProgramID);
        if (error != null) Console.WriteLine("Link shader program: " + error);

        ModelMatrixLocation = gl.GetUniformLocationString(ProgramID, "model_matrix");
        _projectionMatrixLoc = gl.GetUniformLocationString(ProgramID, "projection_matrix");
        _viewMatrixLoc = gl.GetUniformLocationString(ProgramID, "view_matrix");
        _cameraLocationLoc = gl.GetUniformLocationString(ProgramID, "camera_location");
        _envMatrix = gl.GetUniformLocationString(ProgramID, "env_matrix");
    }

    public unsafe void UseProgram(GlInterface gl, Matrix4 view, Matrix4 projection, Vector3 cameraLocation, Matrix4 envMatrix)
    {
        gl.UseProgram(ProgramID);
        gl.UniformMatrix4fv(_viewMatrixLoc, 1, false, (float*)&view);
        gl.UniformMatrix4fv(_projectionMatrixLoc, 1, false, (float*)&projection);
        gl.Uniform3f(_cameraLocationLoc, cameraLocation.X, cameraLocation.Y, cameraLocation.Z);
        gl.UniformMatrix4fv(_envMatrix, 1, false, (float*)&envMatrix);
    }

    public static string LoadShaderFile(string shaderFile)
    {
        ArgumentNullException.ThrowIfNull(shaderFile);

        return File.ReadAllText(shaderFile);
    }
}
