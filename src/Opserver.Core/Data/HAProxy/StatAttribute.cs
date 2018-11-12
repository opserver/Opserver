using System;

namespace StackExchange.Opserver.Data.HAProxy
{
    /// <summary>
    /// Represents a statistic from the proxy stat dump, since these are always added at the end in newer versions, they're parsed based on position.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StatAttribute : Attribute
    {
        public int Position { get; set; }
        public string Name { get; set; }

        public StatAttribute(string name, int position)
        {
            Position = position;
            Name = name;
        }
    }
}
