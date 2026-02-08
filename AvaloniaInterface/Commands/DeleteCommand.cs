using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.Commands;

internal class DeleteCommand : ICommand
{
    public ICommand? Next { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        throw new NotImplementedException();
    }

    public void Redo()
    {
        throw new NotImplementedException();
    }

    public void Undo()
    {
        throw new NotImplementedException();
    }
}
