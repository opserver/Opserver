using System;

namespace StackExchange.Opserver.Data.MongoDB
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MongoDBInfoPropertyAttribute : Attribute
    {
        public string PropertyName;
        public MongoDBInfoPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
