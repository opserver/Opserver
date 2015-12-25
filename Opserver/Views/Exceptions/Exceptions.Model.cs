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
        private int? _shownCount;
        public int ShownCount { get { return _shownCount ?? (_shownCount = Errors.Sum(e => e.DuplicateCount)).Value; } }


        public bool ClearLinkForVisibleOnly { get; set; }

        public bool ShowClearLink
        {
            get { return SelectedLog.HasValue() && Errors.Any(e => !e.IsProtected) && Current.User.IsExceptionAdmin; }
        }

        public int LoadAsyncSize { get; set; }

        public bool ShowDeleted { get; set; }
        public bool ShowAll => SelectedLog.IsNullOrEmpty();
        private int? _totalCount;
        public int TotalCount { get { return _totalCount ?? (_totalCount = Applications.Where(a => ShowAll || a.Name == SelectedLog).Sum(a => a.ExceptionCount)).Value; } }
        public bool HasMore {  get { return TotalCount > ShownCount; } }
        public string Title
        {
            get
            {
                if (Search.HasValue())
                {
                    return $"{Errors.Count} Search results ({Errors.Sum(e => e.DuplicateCount).ToComma()} exceptions) for '{Search}'{(SelectedLog.HasValue() ? " in " + SelectedLog : "")}";
                }
                if (Exception == null)
                {
                    return TotalCount.Pluralize((SelectedLog + " Exception").Trim());
                }
                return $"Most recent {Errors.Count} similar entries ({Errors.Sum(e => e.DuplicateCount).ToComma()} exceptions) from {SelectedLog}";
            }
        }
    }
}