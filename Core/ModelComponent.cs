
namespace Core;

public abstract class ModelComponent
{
    public Model Model { get; set; } = null!;//This should be set by the model when attached!

    public abstract void OnModelUpdate(Model model, UpdateType info); 

    public abstract void OnAddedToModel(Model model);
}
