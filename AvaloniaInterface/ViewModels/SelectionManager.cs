using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace OpenglAvaloniaTest.ViewModels;


public class SelectionManager
{
    public static SelectionManager Instance = new();

    public SelectionMode _SelectionMode = SelectionMode.Face;

    public SelectionMode CurrentSelectionMode
    {
        get => _SelectionMode;
        set {
            SelectionMode oldValue = _SelectionMode;
            _SelectionMode = value;
            AdjustSelection(oldValue);
        }
    }

    private Model? _CurrentModel = null;

    private HashSet<object> _CurrentSelection = new();

    private SelectionManager()
    {
        SceneHierarchy.Instance.OnModelRemoved += OnModelDeleted;
    }

    private void OnModelDeleted(Model obj)
    {
        
        if(obj == _CurrentModel)
        {
            _CurrentModel = null;
            Console.WriteLine("Deleted Selected model");
        }
    }

    private void AdjustSelection(SelectionMode oldValue)
    {
        if (_CurrentModel == null || CurrentSelectionMode == oldValue) return;

        if (CurrentSelectionMode == SelectionMode.Object)//Object mode to model editing mode.
        {
            ClearSelection();
            _CurrentSelection.Clear();
        }
        else
        {
            _CurrentSelection.Clear();
        }
    }

    public void SelectModel(Model model)
    {
        if (_CurrentModel == model) return;

        ClearSelectedModel();
        _CurrentModel = model;
    }

    private void ClearSelectedModel()
    {
        ClearSelection();
        _CurrentModel = null;
    }

    private void ClearSelection()
    {
        if (_CurrentModel == null) return;//Cannot do anything.
        _CurrentModel.GetComponent<ModelSelection>()?.DeselectAll();
    }

    public void CheckForSelection(Vector2 screenPosition)
    {
        RaycastHit? hit = Camera.Instance?.FindRaycastHit(screenPosition);
        if (hit != null)
        {
            SelectModel(hit.Model);
            ModelSelection? ms = hit!.Model.GetComponent<ModelSelection>();

            if (ms == null) throw new InvalidOperationException("Model dosn't contain ModelSelection!");

            if(!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ms.DeselectAll(UpdateType.Ignore);

            foreach (uint index in hit!.Face.Indicies)
            {
                ms.SelectIndex(index, UpdateType.Ignore);
            }
            ms.BroadcastMassUpdate(UpdateType.Face);
        }
        else
        {
            ClearSelectedModel();
        }
    }
}
