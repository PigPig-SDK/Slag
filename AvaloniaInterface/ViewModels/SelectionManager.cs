using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

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

    public Model? CurrentModel { get; private set; } = null;

    private HashSet<object> _CurrentSelection = new();

    private SelectionManager()
    {
        SceneHierarchy.Instance.OnModelRemoved += OnModelDeleted;
    }

    private void OnModelDeleted(HierarchyType hierarchyType, Model obj)
    {
        if(obj == CurrentModel)
        {
            CurrentModel = null;
            Console.WriteLine("Deleted Selected model");
        }
    }

    private void AdjustSelection(SelectionMode oldValue)
    {
        if (CurrentModel == null || CurrentSelectionMode == oldValue) return;

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
        if (CurrentModel == model) return;

        ClearSelectedModel();
        CurrentModel = model;
    }

    private void ClearSelectedModel()
    {
        ClearSelection();
        CurrentModel = null;
    }

    private void ClearSelection()
    {
        if (CurrentModel == null) return;//Cannot do anything.
        CurrentModel.GetComponent<SelectionComponent>()?.DeselectAll();
    }

    public void CheckForSelection(Vector2 screenPosition)
    {
        switch (_SelectionMode)
        {
            case SelectionMode.Face:
                CheckForFaceSelection(screenPosition);
                break;
            case SelectionMode.Vertex:
                CheckForVertexSelection(screenPosition);
                break;
            case SelectionMode.Edge:
                CheckForEdgeSelection(screenPosition);
                break;

        }
    }

    private void CheckForVertexSelection(Vector2 screenPosition)
    {
        if (Camera.Instance == null)
        {
            throw new InvalidOperationException($"{nameof(Camera.Instance)} is not initialzied!");
        }

        VertexHit? hit = Raycast.GetVertexHit(SceneHierarchy.Instance.GetModels(HierarchyType.Model), Camera.Instance.ScreenToGlCoords(screenPosition), Camera.Instance.ViewMatrix);

        if (hit != null)
        {
            SelectModel(hit.Model);
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>();
            if (ms == null) throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");

            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ms.DeselectAll(UpdateType.Ignore);

            ms.SelectIndex(hit.VertexIndex, UpdateType.Ignore);
            ms.BroadcastMassUpdate(UpdateType.Selection);
            _CurrentSelection.Add(hit.VertexIndex);
        }
        else
        {
            ClearSelectedModel();
        }
    }

    private void CheckForEdgeSelection(Vector2 screenPosition)
    {
        if (Camera.Instance == null)
        {
            throw new InvalidOperationException($"{nameof(Camera.Instance)} is not initialzied!");
        }

        EdgeHit? hit = Raycast.GetEdgeHit(
            SceneHierarchy.Instance.GetModels(HierarchyType.Model), 
            Camera.Instance.ScreenToGlCoords(screenPosition), 
            Camera.Instance.ViewMatrix,
            Camera.Instance.Origin);

        if (hit != null)
        {
            SelectModel(hit.Model);
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>();
            if (ms == null) throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");

            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ms.DeselectAll(UpdateType.Ignore);

            ms.SelectIndex(hit.Edge.Vertex1, UpdateType.Ignore);
            ms.SelectIndex(hit.Edge.Vertex2, UpdateType.Ignore);
            ms.BroadcastMassUpdate(UpdateType.Selection);
            _CurrentSelection.Add(hit.Edge);
        }
        else
        {
            ClearSelectedModel();
        }
    }

    private void CheckForFaceSelection(Vector2 screenPosition)
    {
        RaycastHit? hit = Camera.Instance?.FindRaycastHit(screenPosition);
        if (hit != null)
        {
            SelectModel(hit.Model);
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>();

            if (ms == null) throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");

            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ms.DeselectAll(UpdateType.Ignore);

            foreach (uint index in hit!.Face.Indicies)
            {
                ms.SelectIndex(index, UpdateType.Ignore);
            }
            ms.BroadcastMassUpdate(UpdateType.Selection);
            _CurrentSelection.Add(hit.Face);
        }
        else
        {
            ClearSelectedModel();
        }
    }

    public void DeleteCurrentSelection()
    {

        if(CurrentModel == null) return;

        foreach (object obj in _CurrentSelection)
        {
            if (obj is uint index)
            {
                CurrentModel.RemoveVertex((int)index, UpdateType.Ignore);
                Console.WriteLine($"Delete vert {index}");
            }
            else if (obj is Edge edge)
            {
                CurrentModel.RemoveEdge(edge, UpdateType.Ignore);
                Console.WriteLine($"Delete edge {edge}");
            }
            else if (obj is Face face)
            {
                CurrentModel.RemoveFace(face, UpdateType.Ignore);
                Console.WriteLine($"Delete face {face}");
            }
        }

        CurrentModel.UpdateAllComponents(UpdateType.Membership, null);
        _CurrentSelection.Clear();
    }
}
