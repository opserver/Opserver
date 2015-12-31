using System;
using System.Text;
using System.Threading;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Provides optoimized access to StringBuilder instances
    /// Credit: Marc Gravell (@marcgravell), Stack Exchange Inc.
    /// </summary>
    public static class StringBuilderCache
    {
        // one per thread
        [ThreadStatic]
        private static StringBuilder _perThread;
        // and one secondary that is shared between threads
        private static StringBuilder _shared;
        
        private const int DefaultCapacity = 0x10;

        /// <summary>
        /// Obtain a StringBuilder instance; this could be a recycled instance, or could be new
        /// </summary>
        public static StringBuilder Get(int capacity = DefaultCapacity)
        {
            var tmp = _perThread;
            if (tmp != null)
            {
                _perThread = null;
                tmp.Length = 0;
                return tmp;
            }

            tmp = Interlocked.Exchange(ref _shared, null);
            if (tmp == null) return new StringBuilder(capacity);
            tmp.Length = 0;
            return tmp;
        }

        /// <summary>
        /// Get the string contents of a StringBuilder and recyle the instance at the same time
        /// </summary>
        public static string ToStringRecycle(this StringBuilder obj)
        {
            var s = obj.ToString();
            Recycle(obj);
            return s;
        }

        /// <summary>
        /// Get the string contents of a StringBuilder and recyle the instance at the same time
        /// </summary>
        public static string ToStringRecycle(this StringBuilder obj, int startIndex, int length)
        {
            var s = obj.ToString(startIndex, length);
            Recycle(obj);
            return s;
        }

        /// <summary>
        /// Recycles a StringBuilder instance if possible
        /// </summary>
        public static void Recycle(StringBuilder obj)
        {
            if (obj == null) return;
            if (_perThread == null)
            {
                _perThread = obj;
            }
            else
            {
                Interlocked.CompareExchange(ref _shared, obj, null);
            }
        }
    }
}
