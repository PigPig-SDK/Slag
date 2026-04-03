using Avalonia;
using Avalonia.Input;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.ViewModels;

namespace UI.Commands;

public class MergeCommand : MementoCommand
{
    public override string Name => "Merge";
    public override string Description => "Select a vertex to merge into";
    public override bool DisplayToolText => true;

    public override CommandState Execute((KeyEventArgs? keyEvent, PointerEventArgs? mouseEvent, CommandInfo info) args)
    {
        if (SelectionManager.Instance.CurrentModel is null) return CommandState.Discard;
        if (args.mouseEvent is null) return CommandState.Idle;

        SelectionComponent? selection = SelectionManager.Instance.GetSelectionComponent();
        if (selection is null) return CommandState.Discard;

        var properties = args.mouseEvent.GetCurrentPoint(null).Properties;


        if (properties.IsLeftButtonPressed)
        {
            CreateState();
            VertexHit? hit = Raycast.GetVertexHit(
                [selection.Model],
                Camera.Instance.ScreenToGlCoords(args.mouseEvent.GetScreenPos()),
                Camera.Instance.ViewMatrix);

            if (hit is null) return CommandState.Finished;

            uint vertIndex = hit.VertexIndex;

            CreateState();
            Merge(vertIndex, 
                SelectionManager.Instance.CurrentModel, 
                selection);

            return CommandState.Finished;
        }
        return CommandState.Idle;
    }
    public void Merge(uint mergeIntoIndex, Model model, SelectionComponent selection)
    {
        HashSet<uint> selectedIndicies = [..selection.GetSelection<uint>()];

        foreach (Face face in model.Faces.ToArray())
        {
            //See if our face needs to be adjusted.
            bool faceRequiresRebuild = false;
            bool faceContainsMergingIndex = false;
            foreach (uint index in face.Indicies)//Index is to be removed.
            {
                if (selectedIndicies.Contains(index))
                    faceRequiresRebuild = true;
                if(index == mergeIntoIndex) 
                    faceContainsMergingIndex = true;
            }
            if (!faceRequiresRebuild) continue;//Skip the face.

            //Rebuild face with vertex in place
            List<uint> indicies = [.. face.Indicies.ToArray()];//Clone indicies

            //Find closest vertex to implace merge vertex.
            if(!faceContainsMergingIndex)
            {
                //Replace our closest vert with the merge vert, and remove the rest later.
                uint closestIndex = 0;
                for (int cloneArrayIndex = 1; cloneArrayIndex < indicies.Count; cloneArrayIndex++)
                {
                    if (Vector3.DistanceSquared(model.Verticies[(int)indicies[cloneArrayIndex]].Position, model.Verticies[(int)mergeIntoIndex].Position)
                        > Vector3.DistanceSquared(model.Verticies[(int)indicies[cloneArrayIndex]].Position, model.Verticies[(int)mergeIntoIndex].Position))
                        closestIndex = (uint)cloneArrayIndex;
                }
                indicies[(int)closestIndex] = mergeIntoIndex;//Implace merge vertex into face.
            }

            //Remove remaining indicies
            for (int i = indicies.Count - 1; i >= 0; i--)
            {
                if (selectedIndicies.Contains(indicies[i]) && indicies[i] != mergeIntoIndex)
                    indicies.RemoveAt(i);//Remove vert from face
            }

            Console.WriteLine($"Adding new face with indicies: {string.Join(", ", indicies)}");
            if (indicies.Count >= 3)
            {
                model.AddFace(new Face(indicies), UpdateType.None);//Add new face with vert removed.
            }
            model.RemoveFace(face, UpdateType.None);
        }
        model.UpdateAllComponents(UpdateType.Membership);
    }
}
