using Avalonia.Input;
using Core;
using System;
using System.Linq;
using UI.ViewModels;

namespace UI.Commands;

public class MergeCommand : MementoCommand
{
    public ICommand? Next { get; set; }

    public override string Name => throw new NotImplementedException();
    public override string Description => throw new NotImplementedException();
    public override bool ShowUpOToolbar => false;

    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel is null) return CommandState.Discard;

        SelectionComponent? selection = SelectionManager.Instance.GetSelectionComponent();
        if (selection is null) return CommandState.Discard;

        

        return CommandState.Finished;
    }

}
