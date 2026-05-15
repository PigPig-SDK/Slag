using Avalonia.Input;
using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.ViewModels;

namespace UI.Commands;

public class DebugCommand : ICommand
{
    public ICommand? Next { get; set; }

    public string Name => "Information";
    public override string ToString() => Name;

    private string _description = "Click on a primitive to print information about it";
    public string Description => _description;

    public string IconSource => "avares://Slag/Assets/icons/information.png";

    public bool DisplayToolText => true;

    public bool AllowInMeshMode => false;

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {

        if(args.keyEvent != null)
        {
            if(args.keyEvent.Key == Key.Escape)
            {
                return CommandState.Discard;
            }
        }
        if(args.mouseEvent != null)
        {
            string info = "Vertex:\n";

            var properties = args.mouseEvent.GetCurrentPoint(null).Properties;
            if (properties.IsLeftButtonPressed)
            {
                var screenCoordsGL = Camera.Instance.ScreenToGlCoords(args.mouseEvent.GetScreenPos());
                var cameraMatrix = Camera.Instance.ViewMatrix;

                VertexHit? hit = Raycast.GetVertexHit(
                    SceneHierarchy.Instance.GetModels(HierarchyType.Model),
                    screenCoordsGL,
                    cameraMatrix);
                if(hit != null)
                {
                    info += 
                        $"Vertex Index: {hit.VertexIndex}\n" +
                        $"Neighbors: [{string.Join(",", hit.Model.VertexEdgeMap[(int)hit.VertexIndex])}]\n";
                }

                RaycastHit? faceHit = Camera.Instance.FindRaycastHit(args.mouseEvent.GetScreenPos(), SceneHierarchy.Instance.GetModels(HierarchyType.Model));
                if (faceHit != null)
                {
                    info += $"Face Hit:\n{faceHit.Face.ToString()}\n";
                }

                EdgeHit? edgeHit = Raycast.GetEdgeHit(
                    SceneHierarchy.Instance.HierarchyCategories[HierarchyType.Model],
                    screenCoordsGL, cameraMatrix, Camera.Instance.Origin);
                if (edgeHit != null)
                {
                    info += $"Edge Hit:\n{edgeHit.Edge.ToString()}\n";
                }

                SetDescription(info);
            }
        }
        return CommandState.Idle;
    }

    private void SetDescription(string description)
    {
        _description = $"Click on a Vertex/Edge/Face to print information about it\n{description}";
        CommandInvoker.Singleton.UpdateCommandInfo(this);
    }

    public void Redo()
    {
    }

    public void Undo()
    {
    }
}
