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
        private int? _totalCount;
        public int TotalCount { get { return _totalCount ?? (_totalCount = Applications.Where(a => ShowAll || a.Name == SelectedLog).Sum(a => a.ExceptionCount)).Value; } }
        public string Title
        {
            get
            {
                if (Search.HasValue())
                {
                    return string.Format("{0} Search results ({1} exceptions) for '{2}'{3}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), Search, SelectedLog.HasValue() ? " in " + SelectedLog : "");
                }
                if (Exception == null)
                {
                    return TotalCount.Pluralize((SelectedLog + " Exception").Trim());
                }
                return string.Format("Most recent {0} similar entries ({1} exceptions) from {2}", Errors.Count, Errors.Sum(e => e.DuplicateCount).ToComma(), SelectedLog);
            }
        }
    }
}