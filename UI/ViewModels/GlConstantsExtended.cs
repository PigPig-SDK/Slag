using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.ViewModels;

/// <summary>
/// Pulled from:
/// https://javagl.github.io/GLConstantsTranslator/GLConstantsTranslator.html
/// </summary>
public static class GlConstantsExtended
{
#pragma warning disable CA1707
#pragma warning disable CA1823
    public const int GL_UNSIGNED_INT = 0x1405;

    public const int GL_DYNAMIC_DRAW = 0x88E8;

    public const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821a;

    public const int GL_RGBA = 0x1908;

    public const int GL_RGBA8 = 0x8058;

    public const int GL_POINTS = 0x0000;

    public const int GL_LINES = 0x0001;

    public const int GL_LINE_LOOP = 0x0002;

    public const int GL_LINE_STRIP = 0x0003;

    public const int GL_TRIANGLE_STRIP = 0x0005;

    public const int GL_TRIANGLE_FAN = 0x0006;

    public const int GL_LEQUAL = 0x0203;

    public const int GL_ALWAYS = 0x207;

    public const int GL_PROGRAM_POINT_SIZE = 0x8642;

    public const int GL_POLYGON_OFFSET_LINE = 0x2a02;

    public const int GL_TEXTURE_WRAP_S = 10242;

    public const int GL_TEXTURE_WRAP_T = 10243;

    public const int GL_REPEAT = 10497;

    public const int GL_NONE = 0;

    public const int GL_CLAMP_TO_EDGE = 33071;

    public const int GL_DEPTH_COMPONENT24 = 33190;

    public const int GL_TEXTURE_COMPARE_MODE = 34892;

    public const int GL_STENCIL_TEST = 2960;

    public const uint GL_KEEP = 7680;

    public const uint GL_REPLACE = 7681;

    public const uint GL_NOTEQUAL = 517;
}
