using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace Core;

public class SelectionMeshInstance : ModelComponent
{
    private static SelectionMeshInstance? _instance = null!;
    public static SelectionMeshInstance Instance 
    { get
        {
            if(_instance is null) throw new InvalidOperationException($"Please initialzie the {nameof(SelectionMeshInstance)} before trying to access it!");
            return _instance;
        }
        set
        {
            if(_instance is not null) throw new InvalidOperationException($"Cannot set {nameof(SelectionMeshInstance)} more than once!");
            _instance = value;
        }
    }

    private Dictionary<Face, Face> _faceMapper = [];

    public SelectionMeshInstance() 
    {
        Instance = this;
    }

    public override void Dispose() {
        GC.SuppressFinalize(this);
    }
    public override void OnAddedToModel(Model model) { }
    public override void OnModelUpdate(Model model, UpdateType info) { }

    public void SelectFace(Face face)
    {
        return;

        if (_faceMapper.ContainsKey(face)) return;

        if (face.ParentModel is null) return;

        Face localFace = (Face)face.Clone();
        _faceMapper.Add(face, localFace);

        Dictionary<uint, uint> destinationMapper = [];
        foreach (uint index in face.Indices)
        {
            uint destination = Model.AddVertex(face.ParentModel.GetVertex(index), UpdateType.Ignore);
            destinationMapper[index] = destination;
        }
        for(int i = 0; i < localFace.Indices.Count; i++)
        {
            localFace.Indices[i] = destinationMapper[localFace.Indices[i]];
        }
        Model.AddFace(localFace);
    }

    public void DeselectFace(Face face)
    {
        return;
        if (!_faceMapper.TryGetValue(face, out Face? mappedFace)) return;
        if (mappedFace is null) return;

        uint[] indices = mappedFace.Indices.ToArray();
        indices = indices.OrderByDescending(x => x).ToArray();

        foreach (uint index in indices)
        {
            Model.RemoveVertex((int)index);
        }
    }
}
