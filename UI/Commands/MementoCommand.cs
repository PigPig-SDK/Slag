using Avalonia.Input;
using Core;
using UI.ViewModels;
using System;

namespace UI.Commands;

public abstract class MementoCommand : ICommand
{
    public ICommand? Next { get; set; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract bool DisplayToolText { get; }

    public abstract CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args);

    public Model? ModelState;
    public Model? Model;

    protected void CreateState()
    {
        if(SelectionManager.Instance is null)
        {
            throw new InvalidOperationException($"{nameof(SelectionManager.Instance)} is being called while not initialized!");
        }
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
