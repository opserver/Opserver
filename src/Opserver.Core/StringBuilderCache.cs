using System;
using System.Text;
using System.Threading;

namespace StackExchange.Opserver
{
    /// <summary>
    /// Provides optimized access to StringBuilder instances
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
        /// <param name="capacity">The capaity to start the fetched <see cref="StringBuilder"/> at.</param>
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
        /// <param name="builder">The <see cref="StringBuilder"/> to recycle.</param>
        public static string ToStringRecycle(this StringBuilder builder)
        {
            var s = builder.ToString();
            Recycle(builder);
            return s;
        }

        /// <summary>
        /// Get the string contents of a StringBuilder and recycle the instance at the same time
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to recycle.</param>
        /// <param name="startIndex">The index to start at.</param>
        /// <param name="length">The amount of characters to get.</param>
        public static string ToStringRecycle(this StringBuilder builder, int startIndex, int length)
        {
            var s = builder.ToString(startIndex, length);
            Recycle(builder);
            return s;
        }

        /// <summary>
        /// Recycles a StringBuilder instance if possible
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/> to recycle.</param>
        public static void Recycle(StringBuilder builder)
        {
            if (builder == null) return;
            if (_perThread == null)
            {
                _perThread = builder;
            }
            else
            {
                Interlocked.CompareExchange(ref _shared, builder, null);
            }
        }
    }
}
