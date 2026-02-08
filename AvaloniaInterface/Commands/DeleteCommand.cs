using Avalonia.Input;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.Commands;

internal class DeleteCommand : ICommand
{
    public ICommand? Next { get; set; }

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        SelectionManager.Instance.DeleteCurrentSelection();
        return CommandState.Finished;
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
