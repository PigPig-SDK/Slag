using System;

namespace OpenglAvaloniaTest.ViewModels;

[Flags]
public enum UserControlMode
{
    None = 0,
    Shift = 1 << 0,
    Ctrl = 1 << 1,
    Alt = 1 << 2,
}
