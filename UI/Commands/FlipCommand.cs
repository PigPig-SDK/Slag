using Avalonia.Input;
using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Commands;

public class FlipCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Flip";
    public override string ToString() => Name; 

    public string Description => "Flips selected faces";

    public bool DisplayToolText => false;
    
    public string IconSource => "avares://Slag/Assets/icons/flip.png";

    public bool AllowInMeshMode => false;

    private HashSet<Face> _faces = [];
    private Model? _currentModel;

    public CommandState Execute(CommandArguments args)
    {
        _currentModel = SelectionManager.Instance.CurrentModel;

        if (_currentModel is null) return CommandState.Discard;

        SelectionComponent selection = _currentModel.GetComponent<SelectionComponent>()!;

        if(selection.SelectedFaces.Count == 0) return CommandState.Discard;

        _faces = selection.SelectedFaces.ToHashSet();

        ExecuteFlip();

        return CommandState.Finished;
    }

    private void ExecuteFlip()
    {
        if (_currentModel is null) return;

        foreach (Face face in _faces)
        {
            _currentModel.RemoveFace(face);
            face.Indices.Reverse();
            _currentModel.AddFace(face.Indices);
        }
    }

    public void Redo()
    {
        ExecuteFlip();
    }

    public void Undo()
    {
        ExecuteFlip();
    }
}
