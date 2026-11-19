using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Common
{
    [Flags]
    internal enum UnionCharsets : byte
    {
        None = 0,

        Signed = 1,
        Number = 2,
        Stringy = 4,
        Bool = 8,
        Object = 16,
        Listy = 32,

        Null = 128
    }
}
