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

    private string _description = "Click on a vertex to print information about it";
    public string Description => _description;

    public bool DisplayToolText => true;

    public CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {

        if(args.keyEvent != null)
        {
            if(args.keyEvent.Key == Key.Escape)
            {
                Console.WriteLine("Throw away");
                return CommandState.Discard;
            }
        }
        if(args.mouseEvent != null)
        {
            var properties = args.mouseEvent.GetCurrentPoint(null).Properties;
            if (properties.IsLeftButtonPressed)
            {
                VertexHit? hit = Raycast.GetVertexHit(
                    SceneHierarchy.Instance.GetModels(HierarchyType.Model),
                    Camera.Instance.ScreenToGlCoords(args.mouseEvent.GetScreenPos()),
                    Camera.Instance.ViewMatrix);
                if(hit != null)
                {
                    string info = 
                        $"Vertex Index: {hit.VertexIndex}\n" +
                        $"Neighbors: [{string.Join(",", hit.Model.VertexEdgeMap[(int)hit.VertexIndex])}]";

                    SetDescription(info);
                }
            }
        }
        return CommandState.Idle;
    }

    private void SetDescription(string description)
    {
        _description = $"Click on a vertex to print information about it\n{description}";
        CommandInvoker.Singleton.UpdateCommandInfo(this);
    }

    public void Redo()
    {
    }

    public void Undo()
    {
    }
}
