using System;

namespace StackExchange.Opserver.Data.Redis
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RedisInfoPropertyAttribute : Attribute
    {
        public string PropertyName;
        public RedisInfoPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
