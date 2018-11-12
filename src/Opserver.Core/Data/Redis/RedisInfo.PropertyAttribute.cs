using System;

namespace StackExchange.Opserver.Data.Redis
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisInfoPropertyAttribute : Attribute
    {
        public string PropertyName { get; }
        public RedisInfoPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
