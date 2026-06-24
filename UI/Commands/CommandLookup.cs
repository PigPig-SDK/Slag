using Avalonia.Input;
using System;
using System.Collections.Generic;

namespace UI.Commands;

public static class CommandLookup
{
    private static readonly Dictionary<CommandTypes, Func<CommandArguments, ICommand>> _keyBindFactory = new(){
    {CommandTypes.Move, (args) => new MoveCommand(args)},
    {CommandTypes.Rotate, (args) => new RotateCommand(args)},
    {CommandTypes.Scale, (args) => new ScaleCommand(args)},
    {CommandTypes.Extrude, (args) => new ExtrudeCommand()},
    {CommandTypes.Flip, (args) => new FlipCommand()},
    {CommandTypes.Delete, (args) => new DeleteCommand()},
    };
    public static IReadOnlyDictionary<CommandTypes, Func<CommandArguments, ICommand>> CommandFactory => _keyBindFactory;
}
