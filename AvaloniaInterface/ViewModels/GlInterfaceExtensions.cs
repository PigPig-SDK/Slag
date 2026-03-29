using Avalonia.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UI.ViewModels;

public static unsafe class GlInterfaceExtensions
{
    private delegate void Uniform3fDelegate(int location, float x, float y, float z);
    private delegate void LineWidthDelegate(float width);
    private delegate void ReadBufferDelegate(uint glenum);
    private delegate void DrawBuffersDelegate(int n, uint[] bufs);
    private delegate void BufferSubDataDelegate(int target, IntPtr offset, IntPtr size, IntPtr data);
    private delegate void Uniform1iDelegate(int location, int value);

    private static Uniform3fDelegate? _uniform3f;
    private static LineWidthDelegate? _lineWidth;
    private static BufferSubDataDelegate? _bufferSubData;
    private static ReadBufferDelegate? _readBuffer;
    private static DrawBuffersDelegate? _drawBuffers;
    private static Uniform1iDelegate? _uniform1i;

    public static void BufferSubData(this GlInterface gl, int target, IntPtr offset, IntPtr size, IntPtr data)
    {
        if (_bufferSubData == null)
        {
            nint ptr = gl.GetProcAddress("glBufferSubData");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glBufferSubData not available");
            _bufferSubData = Marshal.GetDelegateForFunctionPointer<BufferSubDataDelegate>(ptr);
        }

        _bufferSubData(target, offset, size, data);
    }

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

    public static void ReadBuffer(this GlInterface gl, uint param)
    {
        if (_readBuffer == null)
        {
            nint ptr = gl.GetProcAddress("glReadBuffer");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glReadBuffer not available");
            _readBuffer = Marshal.GetDelegateForFunctionPointer<ReadBufferDelegate>(ptr);
        }
        _readBuffer(param);
    }

    public static void DrawBuffers(this GlInterface gl, int n, uint[] bufs)
    {
        if (_drawBuffers == null)
        {
            nint ptr = gl.GetProcAddress("glDrawBuffers");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glDrawBuffers not available");
            _drawBuffers = Marshal.GetDelegateForFunctionPointer<DrawBuffersDelegate>(ptr);
        }
        _drawBuffers(n, bufs);
    }

    public static void Uniform1i(this GlInterface gl, int location, int value)
    {
        if (_uniform1i == null)
        {
            nint ptr = gl.GetProcAddress("glUniform1i");
            if (ptr == IntPtr.Zero) throw new InvalidOperationException("glUniform1i not available");
            _uniform1i = Marshal.GetDelegateForFunctionPointer<Uniform1iDelegate>(ptr);
        }

        _uniform1i(location, value);
    }
}
