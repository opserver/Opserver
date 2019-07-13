using System;
using System.Runtime.Serialization;

namespace Opserver
{
    public interface IMinVersioned
    {
        [IgnoreDataMember]
        Version MinVersion { get; }
    }
}
