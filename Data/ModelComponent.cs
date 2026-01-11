using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public abstract class ModelComponent
{
    public Model model = null!;//This should be set by the model when attached!

    public abstract void OnModelUpdate(Model model, ModelUpdateType info, object data);
}
