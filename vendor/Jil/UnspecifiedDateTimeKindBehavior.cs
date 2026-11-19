using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil
{
    /// <summary>
    /// Specifies what to convert a DateTime to if it has DateTimeKind.Unspecified as it's Kind.
    /// </summary>
    public enum UnspecifiedDateTimeKindBehavior : byte
    {
        /// <summary>
        /// Indicates that the DateTime is actually in the Local time.
        /// 
        /// This is the default.
        /// </summary>
        IsLocal = 0,
        /// <summary>
        /// Indicates that the DateTime is actually in UTC time.
        /// </summary>
        IsUTC
    }
}
