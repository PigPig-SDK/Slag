using Avalonia.Media;
using Models;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenglAvaloniaTest.ViewModels;


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

    /// <summary>
    /// Object can be:
    /// uint -> The index ID
    /// Face,
    /// Edge.
    /// </summary>
    private HashSet<object> _currentSelection = new();

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

    public void ClearSelection()
    {
        if (CurrentModel == null) return;//Cannot do anything.
        CurrentModel.GetComponent<SelectionComponent>()?.DeselectAll();
        _currentSelection.Clear();
    }

    /// <summary>
    /// Returns a selection of your desired type. Will translate between selection modes automatically.
    /// </summary>
    /// <typeparam name="T"> This will be either a Face, Vertex or Edge.</typeparam>
    /// <remarks> Face -> Edge-> Vertex!  If you desire faces and only edges are selected. Then you get nothing!</remarks>
    /// <returns>A list of <typeparamref name="T"/> which is <see cref="Face"/>, <see cref="uint"/>, or <see cref="Edge"/> depending on what your generic desires.</returns>
    public IEnumerable<T> GetSelection<T>()
    {
        if(typeof(T) == typeof(Face))
        {
            foreach (object selectedObject in _currentSelection)
            {
                if(selectedObject is Face face) yield return (T)(object)face;
            }
        }
        else if(typeof(T) == typeof(Edge))
        {
            //Search for edges
            HashSet<Edge> edges = [];
            foreach (object selectedObject in _currentSelection)
            {
                if (selectedObject is Edge edge)
                    edges.Add(edge);
                else if(selectedObject is Face face)
                {
                    foreach(Edge faceEdge in face.Edges)
                    {
                        edges.Add(faceEdge);
                    }
                }
            }
            //yield those found from search
            foreach(Edge edge in edges)
            {
                yield return (T)(object)edge;
            }
        }
        else if (typeof(T) == typeof(uint))//index
        {
            //Search for verts
            HashSet<uint> verts = [];
            foreach (object selectedObject in _currentSelection)
            {
                if (selectedObject is uint vertex)
                {
                    verts.Add(vertex);
                }
                else if (selectedObject is Face face)
                {
                    foreach (uint index in face.Indicies)
                    {
                        verts.Add(index);
                    }
                }
                else if(selectedObject is  Edge edge)
                {
                    verts.Add(edge.Vertex1);
                    verts.Add(edge.Vertex2);
                }
            }
            //yield those found from search
            foreach (uint index in verts)
            {
                yield return (T)(object)index;
            }
        }
    }

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
            {
                ms.SelectIndex(hit.VertexIndex, UpdateType.Ignore);
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _currentSelection.Add(hit.VertexIndex);
            }
            else if (!isDrag)//Deselect
            {
                ms.DeselectIndex(hit.VertexIndex, UpdateType.Ignore);
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _currentSelection.Remove(hit.VertexIndex);
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

            if(!_currentSelection.Contains(hit.Edge))
            {
                ms.SelectIndex(hit.Edge.Vertex1, UpdateType.Ignore);
                ms.SelectIndex(hit.Edge.Vertex2, UpdateType.Ignore);
                _currentSelection.Add(hit.Edge);
                ms.BroadcastMassUpdate(UpdateType.Selection);
            }
            else if(!isDrag)
            {
                ms.DeselectIndex(hit.Edge.Vertex1, UpdateType.Ignore);
                ms.DeselectIndex(hit.Edge.Vertex2, UpdateType.Ignore);
                _currentSelection.Remove(hit.Edge);
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

            if (!_currentSelection.Contains(hit.Face))
            {
                foreach (uint index in hit!.Face.Indicies)
                {
                    ms.SelectIndex(index, UpdateType.Ignore);
                }

                ms.BroadcastMassUpdate(UpdateType.Selection);
                _currentSelection.Add(hit.Face);
                SelectionMeshInstance.Instance.SelectFace(hit.Face);
            }
            else if (!isDrag)
            {
                foreach (uint index in hit!.Face.Indicies)
                {
                    ms.DeselectIndex(index, UpdateType.Ignore);
                }
                ms.BroadcastMassUpdate(UpdateType.Selection);
                _currentSelection.Remove(hit.Face);
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

        foreach (object obj in _currentSelection)
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
        _currentSelection.Clear();
    }

    public void SetSelection(HashSet<object> indicies)
    {
        _currentSelection = indicies;
    }
}
