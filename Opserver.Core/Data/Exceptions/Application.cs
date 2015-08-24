using System;
using System.Text.RegularExpressions;

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

        private static readonly Regex _shortLogStripRegex = new Regex(@"[^A-Za-z_0-9_\-_\.\/]", RegexOptions.Compiled);
        private string _shortName;

        public string ShortName => _shortName ?? (_shortName = _shortLogStripRegex.Replace(Name, ""));

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