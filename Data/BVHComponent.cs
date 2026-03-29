using OpenTK.Mathematics;

namespace Core;

/// <summary>
/// TODO: Implement
/// Premature optimization, leaving be for now.
/// </summary>
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
                int splitAxis = curVolume.X > curVolume.Y && curVolume.X > curVolume.Z ? 0 :
                curVolume.Y > curVolume.Z ? 1 : 2;

                //Sort indicies in range by axis
                SortBasedOnAxis(splitAxis, node.Start[splitAxis] + (curVolume[splitAxis] / 2.0), range, ref tempIndicies);

                //Create child nodes
                node.left = new BVHNode { parent = node };
                node.right = new BVHNode { parent = node };

                //Split indicies in half
                int leftSize = range.size / 2;
                //Extra element when odd.
                int rightSize = range.size - leftSize;

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

    private void SortBasedOnAxis(int splitAxis, double splitPos, (int start, int size) range, ref List<uint> indicies)
    {
        int i = range.start;
        int j = range.start + range.size;
        while(i < j)
        {
            if (Model.Verticies[(int)indicies[i]].Position[splitAxis] < splitPos)
                i++;
            else
            {
                //Swap
                indicies[i] ^= indicies[j];
                indicies[j] ^= indicies[i];
                indicies[i] ^= indicies[j];
                --j;
            }
        }
    }

    public override void OnAddedToModel(Model model)
    {
        Console.WriteLine("Compute tree");
        ComputeTree();
    }

    public override void OnModelUpdate(Model model, UpdateType info)
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
