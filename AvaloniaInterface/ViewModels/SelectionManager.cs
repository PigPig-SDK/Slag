using Avalonia.Media;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UI.ViewModels;


public class SelectionManager
{
    public static SelectionManager Instance = new();

    private SelectionMode _selectionMode = SelectionMode.Face;

    public static SolidColorBrush SelectionColor = new (new Color(255, 255, 165, 0));

    public SelectionMode CurrentSelectionMode
    {
        get => _selectionMode;
        set {
            SelectionMode oldValue = _selectionMode;
            _selectionMode = value;
            AdjustSelection(oldValue);
        }
    }

    public Model? CurrentModel { get; private set; } = null;

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

    public void ClearSelection() => GetSelectionComponent()?.DeselectAll();
    public SelectionComponent? GetSelectionComponent() => CurrentModel?.GetComponent<SelectionComponent>();
    public void CheckForSelection(Vector2 screenPosition, bool isDrag)
    {
        switch (_selectionMode)
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
                ms.SelectIndex(hit.VertexIndex, UpdateType.Selection);
            else if (!isDrag)//Deselect
                ms.DeselectIndex(hit.VertexIndex, UpdateType.Selection);
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

            if(!ms.IsEdgeSelected(hit.Edge))
                ms.SelectEdge(hit.Edge, UpdateType.Selection);
            else if(!isDrag)
                ms.DeselectEdge(hit.Edge, UpdateType.Selection);
            
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

            if (!ms.IsFaceSelected(hit.Face))
            {
                ms.SelectFace(hit!.Face, UpdateType.Selection);
                SelectionMeshInstance.Instance.SelectFace(hit.Face);
            }
            else if (!isDrag)
            {
                ms.DeselectFace(hit!.Face, UpdateType.Selection);
                SelectionMeshInstance.Instance.DeselectFace(hit.Face);
            }
        }
        else if(!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            ClearSelectedModel();
        }
    }
    /// <summary>
    /// Removes the current selection.
    /// </summary>
    public void DeleteCurrentSelection()
    {
        if(CurrentModel == null) return;
        SelectionComponent? component = CurrentModel.GetComponent<SelectionComponent>();
        if (component is null) return;

        var selectionBucket = component!.SelectionBucket().ToArray();
        component.DeselectAll();
        foreach (object obj in selectionBucket)
        {
            if (obj is uint index)
            {
                Console.WriteLine("index : " + index);
                CurrentModel.RemoveVertex((int)index, UpdateType.Ignore);
            }
            else if (obj is Edge edge)
            {
                Console.WriteLine("edge : " + edge);
                CurrentModel.RemoveEdge(edge, UpdateType.Ignore);
            }
            else if (obj is Face face)
            {
                Console.WriteLine("face : " + face);
                CurrentModel.RemoveFace(face, UpdateType.Ignore);
            }
        }

        CurrentModel.UpdateAllComponents(UpdateType.Membership);
    }

    /// <summary>
    /// Set the selection to a hashset
    /// </summary>
    /// <typeparam name="T">Face, uint or edge.</typeparam>
    public void SetSelection<T>(HashSet<T> items)
    {
        SelectionComponent? ms = CurrentModel?.GetComponent<SelectionComponent>();
        if (ms == null) return;
        ms.DeselectAll(UpdateType.Ignore);
        foreach(T obj in items)
        {
            if (obj is uint index) ms.SelectIndex(index, UpdateType.Ignore);
            else if (obj is Edge edge) ms.SelectEdge(edge, UpdateType.Ignore);
            else if (obj is Face face) ms.SelectFace(face, UpdateType.Ignore);
        }
        ms.BroadcastMassUpdate(UpdateType.Selection);
    }
}
