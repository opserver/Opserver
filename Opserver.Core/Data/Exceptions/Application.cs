using System;
using System.Linq;
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

        public int ExceptionCount
        {
            get { return _exceptionCount - IssueCount; }
            internal set { _exceptionCount = value; }
        }

        public int IssueCount
        {
            get
            {
                var issues = Store.Issues.SafeData();
                if (issues == null)
                    return 0;

                ApplicationIssue issue = issues.FirstOrDefault(a => a.Application == Name);
                if (issue == null)
                    return 0;

                return issue.IssueCount;
            }
        }

        public int RecentExceptionCount
        {
            get { return _recentExceptionCount - RecentIssueCount; }
            internal set { _recentExceptionCount = value; }
        }

        public int RecentIssueCount
        {
            get
            {
                var issues = Store.Issues.SafeData();
                if (issues == null)
                    return 0;

                ApplicationIssue issue = issues.FirstOrDefault(a => a.Application == Name);
                if (issue == null)
                    return 0;

                return issue.RecentIssueCount;
            }
        }

        public DateTime? MostRecent { get; internal set; }

        private static readonly Regex _shortLogStripRegex = new Regex(@"[^A-Za-z_0-9_\-_\.\/]", RegexOptions.Compiled);
        private string _shortName;
        private int _exceptionCount;
        private int _recentExceptionCount;

        public string ShortName
        {
            get { return _shortName ?? (_shortName = _shortLogStripRegex.Replace(Name, "")); }
        }

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