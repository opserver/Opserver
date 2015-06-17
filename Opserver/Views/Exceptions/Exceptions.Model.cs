using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using StackExchange.Exceptional;
using StackExchange.Opserver.Data.Exceptions;

namespace StackExchange.Opserver.Views.Exceptions
{
    public class ExceptionsModel
    {
        private NameValueCollection _requestQueryString;
        public NameValueCollection RequestQueryString
        {
            get { return _requestQueryString ?? (_requestQueryString = HttpUtility.ParseQueryString(Current.Request.QueryString.ToString())); }
        }

        public ExceptionSorts Sort { get; set; }
        public string SelectedLog { get; set; }
        public bool ShowingWindow { get; set; }

        public string Search { get; set; }
        public Error Exception { get; set; }
        public List<Application> Applications { get; set; }
        public List<Error> Errors { get; set; }

        public bool ClearLinkForVisibleOnly { get; set; }

        public bool ShowClearLink
        {
            get { return SelectedLog.HasValue() && Errors.Any(e => !e.IsProtected) && Current.User.IsExceptionAdmin; }
        }

        public int LoadAsyncSize { get; set; }

        public bool ShowDeleted { get; set; }
        public bool ShowAll { get { return SelectedLog.IsNullOrEmpty(); } }
        private int? _totalExceptionCount, _totalIssueCount;
        public int TotalExceptionCount { get { return _totalExceptionCount ?? (_totalExceptionCount = Applications.Where(a => ShowAll || a.Name == SelectedLog).Sum(a => a.ExceptionCount)).Value; } }
        public int TotalIssueCount { get { return _totalIssueCount ?? (_totalIssueCount = Applications.Where(a => ShowAll || a.Name == SelectedLog).Sum(a => a.IssueCount)).Value; } }
        public string ExceptionTitle
        {
            get
            {
                if (Search.HasValue())
                {
                    return string.Format("{0} Search results ({1} exceptions) for '{2}'{3}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), Search, SelectedLog.HasValue() ? " in " + SelectedLog : "");
                }
                if (Exception == null)
                {
                    return TotalExceptionCount.Pluralize((SelectedLog + " Exception").Trim());
                }
                return string.Format("Most recent {0} similar entries ({1} exceptions) from {2}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), SelectedLog);
            }
        }

        public string IssueTitle
        {
            get
            {
                if (Search.HasValue())
                {
                    return string.Format("{0} Search results ({1} exceptions) for '{2}'{3}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), Search, SelectedLog.HasValue() ? " in " + SelectedLog : "");
                }
                if (Exception == null)
                {
                    return TotalIssueCount.Pluralize((SelectedLog + " Issue").Trim());
                }
                return string.Format("Most recent {0} similar entries ({1} issues) from {2}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), SelectedLog);
            }
        }
    }
}