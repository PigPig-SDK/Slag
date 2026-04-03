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
        Console.WriteLine($"Selected face {face}");
    }

    public void DeselectFace(Face face)
    {
        Console.WriteLine($"Selected face {face}");
    }
}
