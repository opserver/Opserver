using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Common
{
    static class UnionCharsetArrays
    {
        public static readonly IEnumerable<char> UnionSignedSet = new[] { '-' };
        public static readonly IEnumerable<char> UnionNumberSet = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
        public static readonly IEnumerable<char> UnionStringySet = new[] { '"' };
        public static readonly IEnumerable<char> UnionBoolSet = new[] { 't', 'f' };
        public static readonly IEnumerable<char> UnionObjectSet = new[] { '{' };
        public static readonly IEnumerable<char> UnionListySet = new[] { '[' };

        /// <summary>
        /// Special case, this shouldn't be used in conjuction with types like string or int?; only for the exact null value.
        /// </summary>
        public static readonly IEnumerable<char> UnionNull = new[] { 'n' };
    }
}
