using Avalonia.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.ViewModels;

public static unsafe class GlInterfaceExtensions
{
    private delegate void Uniform3fDelegate(int location, float x, float y, float z);
    private delegate void LineWidthDelegate(float width);

    private static Uniform3fDelegate? _uniform3f;
    private static LineWidthDelegate? _lineWidth;

    public static void Uniform3f(this GlInterface gl, int location, float x, float y, float z)
    {
        if (_uniform3f == null)
        {
            nint ptr = gl.GetProcAddress("glUniform3f");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glUniform3f not available");
            _uniform3f = Marshal.GetDelegateForFunctionPointer<Uniform3fDelegate>(ptr);
        }

        _uniform3f(location, x, y, z);
    }

    public static void LineWidth(this GlInterface gl, float width)
    {
        if (_lineWidth == null)
        {
            nint ptr = gl.GetProcAddress("glLineWidth");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glLineWidth not available");
            _lineWidth = Marshal.GetDelegateForFunctionPointer<LineWidthDelegate>(ptr);
        }
        
        _lineWidth(width);
    }
}
