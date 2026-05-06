using Avalonia.Input;
using Core;
using UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Commands;

internal class DeleteCommand : MementoCommand
{
    public override string Name => "Delete";
    public override string ToString() => Name;
    public override string Description => "Removes your current selection";

    public override string IconSource => "./Assets/edge.png";

    public override bool DisplayToolText => false;
    public override bool AllowInMeshMode => true;
    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        CreateState();
        SelectionManager.Instance.DeleteCurrentSelection();
        return CommandState.Finished;
    }
}
