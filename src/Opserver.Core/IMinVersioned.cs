using System;
using System.Runtime.Serialization;

namespace StackExchange.Opserver
{
    public interface IMinVersioned
    {
        [IgnoreDataMember]
        Version MinVersion { get; }
    }
}
