using Avalonia.Input;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenglAvaloniaTest.Commands;

internal class DeleteCommand : MementoCommand
{
    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        CreateState();
        SelectionManager.Instance.DeleteCurrentSelection();
        return CommandState.Finished;
    }
}
