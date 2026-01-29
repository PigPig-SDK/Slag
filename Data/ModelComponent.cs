using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public abstract class ModelComponent : IDisposable
{
    public Model Model = null!;//This should be set by the model when attached!

    ~ModelComponent() { Dispose(); }

    public abstract void OnModelUpdate(Model model, UpdateType info, object? data);

    public abstract void Dispose();

    public abstract void OnAddedToModel(Model model);
}
