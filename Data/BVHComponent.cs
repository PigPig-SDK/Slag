using OpenglAvaloniaTest.ViewModels;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class BVHComponent : ModelComponent
{
    private BVHNode _root = new();

    //10 verts per branch during tree building.
    //A BVHNode can have more than 10 verts during runtime (faces being created)
    private const int MaxBranchSize = 10;

    public void ClearTree()
    {
        _root = new();
    }

    public void ComputeTree()
    {
        if (_root.left != null || _root.right != null)
        {
            throw new InvalidOperationException($"{nameof(ComputeTree)} called when tree has not been cleared!");
        }

        Queue<BVHNode> pending = new Queue<BVHNode>();
        pending.Enqueue( _root );
        List<uint> tempIndicies = [.. Model.Indicies];
        Dictionary<BVHNode, (int start, int size)> tempMapping = [];
        tempMapping[_root] = (0,tempIndicies.Count - 1);
        int splitAxis = 0;

        //Define comparson by axis for sorting
        var compare = Comparer<uint>.Create((a, b) => { return Model.TryGetVertex(a)!.Value.Position[splitAxis].CompareTo(Model.TryGetVertex(b)!.Value.Position[splitAxis]);});

        while (pending.Count > 0)
        {
            BVHNode node = pending.Dequeue();
            //Compute size
            var range = tempMapping[node];
            node.ComputeSize(Model, tempIndicies, range);

            //Node is too lge, split it
            if (tempMapping[node].size > MaxBranchSize)
            {
                Vector3 curVolume = node.End - node.Start;
                //Find largest axis
                splitAxis = curVolume.X > curVolume.Y && curVolume.X > curVolume.Z ? 0 :
                curVolume.Y > curVolume.Z ? 1 : 2;

                //Sort indicies in range by axis
                tempIndicies.Sort(range.start, range.start + range.size, compare);

                //Create child nodes
                node.left = new BVHNode { parent = node };
                node.right = new BVHNode { parent = node };

                //Split indicies in half
                int leftSize = range.size / 2;
                //Extra element when odd.
                int rightSize = range.size - leftSize;
                Console.WriteLine($"{leftSize} : {rightSize}");

                tempMapping[node.left] = (range.start, leftSize);
                tempMapping[node.right] = (range.start + leftSize, rightSize);

                //Enqueue children
                pending.Enqueue(node.left);
                pending.Enqueue(node.right);
            }
            else//Just right.
            {
                //Push data into node...
                foreach (uint index in tempIndicies.GetRange(range.start, range.size))
                {
                    node.AddIndex(Model, index, false);
                }
            }
        }
    }

    public override void OnAddedToModel(Model model)
    {
        Console.WriteLine("Compute tree");
        ComputeTree();
    }

    public override void OnModelUpdate(Model model, UpdateType info, object? data)
    {

    }

    public override void Dispose() { }

    public static bool BindComponent(Model model)
    {
        if (!model.HasComponent(typeof(BVHComponent)))
        {
            return model.AddComponent<BVHComponent>(new BVHComponent()) != null;
        }
        return false;
    }
}
