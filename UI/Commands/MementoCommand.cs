using Avalonia.Input;
using Core;
using UI.ViewModels;
using System;

namespace UI.Commands;

public abstract class MementoCommand : ICommand
{
    public ICommand? Next { get; set; }

    public abstract string Name { get; }
    public override string ToString() => Name;

    public abstract string Description { get; }

    public abstract bool DisplayToolText { get; }

    public abstract CommandState Execute(CommandArguments args);

    public Model? ModelState { get; set; }
    public Model? Model { get; set; }

    public abstract string IconSource {get;}
    public abstract bool AllowInMeshMode { get; }

    protected void CreateState()
    {
        Model = SelectionManager.Instance.CurrentModel;
        ModelState = Model?.Clone();
    }

    public void Redo()
    {
        if (ModelState == null)
        {
            throw new InvalidOperationException($"{nameof(Redo)} cannot be executed while {nameof(Model)} is null!");
        }

        Model? modelStateTemp = ModelState;
        ModelState = Model?.Clone();
        Model?.EmplaceData(modelStateTemp);
    }

    public void Undo()
    {
        if(ModelState == null)
        {
            throw new InvalidOperationException($"{nameof(Undo)} cannot be executed while {nameof(Model)} is null!");
        }

        Model? modelStateTemp = ModelState;
        ModelState = Model?.Clone();//Store final pos
        Model?.EmplaceData(modelStateTemp);
    }
}
