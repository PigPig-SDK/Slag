using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;
[Flags]
public enum ModelUpdateType
{
    None = 0,
    Vertex = 1 << 0,
    Edge  = 1 << 1,
    Face = 1 << 2,
    Locational = 1 << 2,
    Membership = 1 << 3,
    MassOperation = 1 << 3,
    ComponentAddition = 1 << 4,
}
