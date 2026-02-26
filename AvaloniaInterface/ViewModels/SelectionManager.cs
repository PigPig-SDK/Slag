using Avalonia.Rendering.Composition;
using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenglAvaloniaTest.ViewModels;


public class SelectionManager
{
    public static SelectionManager Instance = new();

    private SelectionMode _SelectionMode = SelectionMode.Face;

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
        _CurrentSelection.Clear();
    }

    public void CheckForSelection(Vector2 screenPosition, bool isDrag)
    {
        switch (_SelectionMode)
        {
            case SelectionMode.Face:
                CheckForFaceSelection(screenPosition, isDrag);
                break;
            case SelectionMode.Vertex:
                CheckForVertexSelection(screenPosition, isDrag);
                break;
            case SelectionMode.Edge:
                CheckForEdgeSelection(screenPosition, isDrag);
                break;

        }
    }

    private void CheckForVertexSelection(Vector2 screenPosition, bool isDrag)
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
                ClearSelection();

            //Select
            if (!ms.IsVertexSelected(hit.VertexIndex))
            {
                ms.SelectIndex(hit.VertexIndex, UpdateType.Ignore);
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _CurrentSelection.Add(hit.VertexIndex);
            }
            else if (!isDrag)//Deselect
            {
                ms.DeselectIndex(hit.VertexIndex, UpdateType.Ignore);
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _CurrentSelection.Remove(hit.VertexIndex);
            }
        }
        else if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            ClearSelectedModel();
        }
    }

    private void CheckForEdgeSelection(Vector2 screenPosition, bool isDrag)
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
                ClearSelection();

            if(!_CurrentSelection.Contains(hit.Edge))
            {
                ms.SelectIndex(hit.Edge.Vertex1, UpdateType.Ignore);
                ms.SelectIndex(hit.Edge.Vertex2, UpdateType.Ignore);
                _CurrentSelection.Add(hit.Edge);
                ms.BroadcastMassUpdate(UpdateType.Selection);
            }
            else if(!isDrag)
            {
                ms.DeselectIndex(hit.Edge.Vertex1, UpdateType.Ignore);
                ms.DeselectIndex(hit.Edge.Vertex2, UpdateType.Ignore);
                _CurrentSelection.Remove(hit.Edge);
                ms.BroadcastMassUpdate(UpdateType.Selection);
            }
            
        }
        else if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            ClearSelectedModel();
        }
    }

    private void CheckForFaceSelection(Vector2 screenPosition, bool isDrag)
    {
        if(SelectionMeshInstance.Instance == null)
        {
            throw new InvalidOperationException($"Cannot check for face selection while {nameof(SelectionMeshInstance.Instance)} is null");
        }

        RaycastHit? hit = Camera.Instance?.FindRaycastHit(screenPosition);
        if (hit != null)
        {
            SelectModel(hit.Model);
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>();

            if (ms == null) throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");

            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
            {
                ClearSelection();
            }

            if (!_CurrentSelection.Contains(hit.Face))
            {
                foreach (uint index in hit!.Face.Indicies)
                {
                    ms.SelectIndex(index, UpdateType.Ignore);
                }

                ms.BroadcastMassUpdate(UpdateType.Selection);
                _CurrentSelection.Add(hit.Face);
                SelectionMeshInstance.Instance.SelectFace(hit.Face);
            }
            else if (!isDrag)
            {
                foreach (uint index in hit!.Face.Indicies)
                {
                    ms.DeselectIndex(index, UpdateType.Ignore);
                }
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _CurrentSelection.Remove(hit.Face);
                SelectionMeshInstance.Instance.DeselectFace(hit.Face);
            }
        }
        else if(!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
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
                //Console.WriteLine($"Delete vert {index}");
            }
            else if (obj is Edge edge)
            {
                CurrentModel.RemoveEdge(edge, UpdateType.Ignore);
                //Console.WriteLine($"Delete edge {edge}");
            }
            else if (obj is Face face)
            {
                CurrentModel.RemoveFace(face, UpdateType.Ignore);
                //Console.WriteLine($"Delete face {face}");
            }
        }

        CurrentModel.UpdateAllComponents(UpdateType.Membership, null);
        _CurrentSelection.Clear();
    }
}
