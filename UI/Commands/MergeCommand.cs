using Avalonia;
using Avalonia.Input;
using Core;
using System;
using System.Linq;
using UI.ViewModels;

namespace UI.Commands;

public class MergeCommand : MementoCommand
{
    public ICommand? Next { get; set; }

    public override string Name => "Merge";
    public override string Description => "Select a vertex to merge into";
    public override bool DisplayToolText => true;

    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel is null) return CommandState.Discard;
        if (args.mouseEvent is null) return CommandState.Idle;

        SelectionComponent? selection = SelectionManager.Instance.GetSelectionComponent();
        if (selection is null) return CommandState.Discard;

        var properties = args.mouseEvent.GetCurrentPoint(null).Properties;


        if (properties.IsLeftButtonPressed)
        {
            CreateState();
            VertexHit? hit = Raycast.GetVertexHit(
                [selection.Model],
                Camera.Instance.ScreenToGlCoords(args.mouseEvent.GetScreenPos(GLControl.Instance!)),
                Camera.Instance.ViewMatrix);

            if (hit is null) return CommandState.Finished;

            uint vertIndex = hit.VertexIndex;

            CreateState();
            Merge(
                vertIndex, 
                SelectionManager.Instance.CurrentModel, 
                selection);

            return CommandState.Finished;
        }
        return CommandState.Idle;
    }
    public void Merge(uint vertIndex, Model model, SelectionComponent selection)
    {
        
    }
}
