using System;

namespace StackExchange.Opserver.Data.Exceptions
{        
    /// <summary>
    /// Represents an application and its store location
    /// </summary>
    public class Application
    {
        public string Name { get; internal set; }
        public string StoreName { get; internal set; }
        public ExceptionStore Store { get; internal set; }

        public int ExceptionCount { get; internal set; }
        public int RecentExceptionCount { get; internal set; }
        public DateTime? MostRecent { get; internal set; }
        
        public JSONApplication ToJSON()
        {
            return new JSONApplication
                       {
                           Name = Name,
                           ExceptionCount = ExceptionCount,
                           MostRecent = MostRecent.ToRelativeTime()
                       };
        }
    }

    public class JSONApplication
    {
        public string Name { get; internal set; }
        public int ExceptionCount { get; internal set; }
        public string MostRecent { get; internal set; }
    }
}