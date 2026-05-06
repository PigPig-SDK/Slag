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
    public static SelectionManager Instance { get; } = new();

    private SelectionMode _selectionMode = SelectionMode.Mesh;

    public static SolidColorBrush SelectionColor => new (new Color(255, 255, 165, 0));

    public float SnapValue { get; set; }

    public SelectionMode CurrentSelectionMode
    {
        get => _selectionMode;
        set {
            SelectionMode oldValue = _selectionMode;
            _selectionMode = value;
            AdjustSelection(oldValue);
        }
    }

    public Model? CurrentModel { get; private set; }
    private readonly List<Model> _currentBroadModels = [];
    /// <summary>
    /// Broad models are 'models selected not in object mode'
    /// </summary>
    public IReadOnlyList<Model> CurrentBroadModels => _currentBroadModels;

    private SelectionManager()
    {
        SceneHierarchy.Instance.OnModelRemoved += OnModelDeleted;
        SceneHierarchy.Instance.SelectedSetReference = _currentBroadModels;
    }

    private void OnModelDeleted(HierarchyType hierarchyType, Model obj)
    {
        if(obj == CurrentModel)
        {
            CurrentModel = null;
        }
    }

    private void AdjustSelection(SelectionMode oldValue)
    {
        if (CurrentModel == null || CurrentSelectionMode == oldValue) return;

        if (oldValue == SelectionMode.Mesh && CurrentSelectionMode != SelectionMode.Mesh)//Switching from mesh to other
            CurrentModel = _currentBroadModels.LastOrDefault();

        if (CurrentSelectionMode == SelectionMode.Mesh)//Object mode to model editing mode.
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
            case SelectionMode.Mesh:
                CheckForMeshSelection(screenPosition, isDrag);
                break;

        }
    }

    private void CheckForMeshSelection(Vector2 screenPosition, bool isDrag)
    {
        RaycastHit? hit = Camera.Instance.FindRaycastHit(screenPosition);
        if (hit != null)
        {
            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                _currentBroadModels.Clear();
            _currentBroadModels.Add(hit.Model);
            SelectModel(hit.Model);
        }
        else if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            _currentBroadModels.Clear();
        }
    }

    private void CheckForVertexSelection(Vector2 screenPosition, bool isDrag)
    {
        VertexHit? hit = Raycast.GetVertexHit(SceneHierarchy.Instance.GetModels(HierarchyType.Selected),
            Camera.Instance.ScreenToGlCoords(screenPosition), Camera.Instance.ViewMatrix);

        if (hit != null)
        {
            if (hit.Model != CurrentModel) return;
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>() ?? throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");
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
            ClearSelection();
        }
    }
    private void CheckForEdgeSelection(Vector2 screenPosition, bool isDrag)
    {
        EdgeHit? hit = Raycast.GetEdgeHit(
            SceneHierarchy.Instance.GetModels(HierarchyType.Selected), 
            Camera.Instance.ScreenToGlCoords(screenPosition), 
            Camera.Instance.ViewMatrix,
            Camera.Instance.Origin);

        if (hit != null)
        {
            if (hit.Model != CurrentModel) return;
            SelectModel(hit.Model);
            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>() ?? throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!"); ;
            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ClearSelection();

            if(!ms.IsEdgeSelected(hit.Edge))
                ms.SelectEdge(hit.Edge, UpdateType.Selection);
            else if(!isDrag)
                ms.DeselectEdge(hit.Edge, UpdateType.Selection);
            
        }
        else if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            ClearSelection();
        }
    }

    private void CheckForFaceSelection(Vector2 screenPosition, bool isDrag)
    {
        RaycastHit? hit = Camera.Instance.FindRaycastHit(screenPosition, HierarchyType.Selected);
        if (hit != null)
        {
            if (hit.Model != CurrentModel) return;

            SelectionComponent? ms = hit!.Model.GetComponent<SelectionComponent>() ?? throw new InvalidOperationException($"Model dosn't contain {nameof(SelectionComponent)}!");
            if (!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))//Not a CTRL selection.
                ClearSelection();

            if (!ms.IsFaceSelected(hit.Face))
            {
                ms.SelectFace(hit!.Face, UpdateType.Selection);
            }
            else if (!isDrag)
            {
                ms.DeselectFace(hit!.Face, UpdateType.Selection);
            }
        }
        else if(!InputManager.Singleton.UserControlMode.HasFlag(UserControlMode.Ctrl))
        {
            ClearSelection();
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
                CurrentModel.RemoveVertex((int)index, UpdateType.Ignore);
            }
            else if (obj is Edge edge)
            {
                CurrentModel.RemoveEdge(edge, UpdateType.Ignore);
            }
            else if (obj is Face face)
            {
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
    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if selection was updated, Else false</returns>
    /// <remarks>Use the fact this returns a bool to implement unselect all</remarks>
    public bool SelectAll()
    {
        if (CurrentSelectionMode == SelectionMode.Mesh)
        {
            int oldSelectCount = _currentBroadModels.Count;

            _currentBroadModels.Clear();
            foreach(Model model in SceneHierarchy.Instance.GetModels(HierarchyType.Model))
                _currentBroadModels.Add(model);

            return oldSelectCount != _currentBroadModels.Count;
        }
        else
        {
            if (CurrentModel is null) return false;//Cannot select anything. This return type is questionable.
            SelectionComponent? selection = CurrentModel.GetComponent<SelectionComponent>();
            if(selection == null) return false;
            
            //the rule for refactoring is 3 repeats. Pray for me.
            switch(CurrentSelectionMode)
            {
                case SelectionMode.Edge:
                    int oldSelectCount = selection.SelectedEdges.Count;
                    selection.DeselectAll(UpdateType.Ignore);
                    foreach(Edge edge in  selection.Model.Edges)
                    {
                        selection.SelectEdge(edge, UpdateType.Ignore);
                    }
                    selection.BroadcastMassUpdate(UpdateType.Selection);

                    return selection.SelectedEdges.Count != oldSelectCount;
                case SelectionMode.Face:
                    oldSelectCount = selection.SelectedFaces.Count;
                    selection.DeselectAll(UpdateType.Ignore);
                    foreach (Face face in selection.Model.Faces)
                    {
                        selection.SelectFace(face, UpdateType.Ignore);
                    }
                    selection.BroadcastMassUpdate(UpdateType.Selection);

                    return selection.SelectedFaces.Count != oldSelectCount;
                case SelectionMode.Vertex:
                    oldSelectCount = selection.SelectedIndices.Count;
                    selection.DeselectAll(UpdateType.Ignore);
                    foreach(uint index in CurrentModel.Indices)
                    {
                        selection.SelectIndex(index, UpdateType.Ignore);
                    }
                    selection.BroadcastMassUpdate(UpdateType.Selection);

                    return selection.SelectedFaces.Count != oldSelectCount;
            }
        }
        throw new NotImplementedException("Due to a new implementation, selection has broken!");
    }
}
