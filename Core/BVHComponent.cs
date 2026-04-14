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

        Queue<BVHNode> pending = new();
        pending.Enqueue( _root );
        List<uint> tempIndices = [.. Model.Indices];
        Dictionary<BVHNode, (int start, int size)> tempMapping = [];
        tempMapping[_root] = (0,tempIndices.Count - 1);

        while (pending.Count > 0)
        {
            BVHNode node = pending.Dequeue();
            //Compute size
            var range = tempMapping[node];
            node.ComputeSize(Model, tempIndices, range);

            //Node is too lge, split it
            if (tempMapping[node].size > MaxBranchSize)
            {
                Vector3 curVolume = node.End - node.Start;
                //Find largest axis
                int splitAxis = curVolume.X > curVolume.Y && curVolume.X > curVolume.Z ? 0 :
                curVolume.Y > curVolume.Z ? 1 : 2;

                //Sort indices in range by axis
                SortBasedOnAxis(splitAxis, node.Start[splitAxis] + (curVolume[splitAxis] / 2.0), range, ref tempIndices);

                //Create child nodes
                node.left = new BVHNode { parent = node };
                node.right = new BVHNode { parent = node };

                //Split indices in half
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
                foreach (uint index in tempIndices.GetRange(range.start, range.size))
                {
                    node.AddIndex(Model, index, false);
                }
            }
        }
    }

    private void SortBasedOnAxis(int splitAxis, double splitPos, (int start, int size) range, ref List<uint> indices)
    {
        int i = range.start;
        int j = range.start + range.size;
        while(i < j)
        {
            if (Model.Verticies[(int)indices[i]].Position[splitAxis] < splitPos)
                i++;
            else
            {
                //Swap
                indices[i] ^= indices[j];
                indices[j] ^= indices[i];
                indices[i] ^= indices[j];
                --j;
            }
        }
    }

    public override void OnAddedToModel(Model model)
    {
        ComputeTree();
    }

    public override void OnModelUpdate(Model model, UpdateType info)
    {

    }

    public override void Dispose() { 
        GC.SuppressFinalize(this);
    }

    public static bool BindComponent(Model model)
    {
        return !model.HasComponent(typeof(BVHComponent)) && model.AddComponent<BVHComponent>(new BVHComponent()) != null;
    }
}
