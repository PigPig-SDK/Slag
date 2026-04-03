
namespace Core;

public abstract class ModelComponent : IDisposable
{
    public Model Model { get; set; } = null!;//This should be set by the model when attached!

    ~ModelComponent() { 
        Dispose();
    }

    public abstract void OnModelUpdate(Model model, UpdateType info); 

    public abstract void Dispose();

    public abstract void OnAddedToModel(Model model);
}
