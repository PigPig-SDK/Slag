using Avalonia.Input;
using System;
using System.Collections.Generic;

namespace UI.Commands;

public static class CommandLookup
{
    private static readonly Dictionary<CommandTypes, Func<ICommand>> _keyBindFactory = new(){
    {CommandTypes.Move, () => new MoveCommand()},
    {CommandTypes.Rotate, () => new RotateCommand()},
    {CommandTypes.Scale, () => new ScaleCommand()},
    {CommandTypes.Extrude, () => new ExtrudeCommand()},
    {CommandTypes.Flip, () => new FlipCommand()},
    {CommandTypes.Delete, () => new DeleteCommand()},
    };
    public static IReadOnlyDictionary<CommandTypes, Func<ICommand>> CommandFactory => _keyBindFactory;
}
