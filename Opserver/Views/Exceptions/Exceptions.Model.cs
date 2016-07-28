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
        public NameValueCollection RequestQueryString => _requestQueryString ?? (_requestQueryString = HttpUtility.ParseQueryString(Current.Request.QueryString.ToString()));

        public ExceptionSorts Sort { get; set; }
        public string SelectedGroup { get; set; }
        public string SelectedLog { get; set; }
        public bool ShowingWindow { get; set; }

        public string Search { get; set; }
        public Error Exception { get; set; }
        public List<ApplicationGroup> Groups { get; set; }
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
        public bool ShowAll => SelectedGroup.IsNullOrEmpty() && SelectedLog.IsNullOrEmpty();
        private int? _totalCount;
        public int TotalCount => _totalCount ?? (_totalCount = GetTotal()).Value;
        private int GetTotal()
        {
            if (!SelectedGroup.HasValue())
            {
                return ExceptionStores.TotalExceptionCount;
            }
            var group = Groups.FirstOrDefault(g => g.Name == SelectedGroup);
            if (group != null && SelectedLog.HasValue())
            {
                return group.Applications.FirstOrDefault(a => a.Name == SelectedLog)?.ExceptionCount ?? 0;
            }
            return group?.Total ?? 0;
        }

        public bool HasMore => TotalCount > ShownCount;
        public string Title
        {
            get
            {
                if (Search.HasValue())
                {
                    return $"{Errors.Count.ToComma()} Search results ({Errors.Sum(e => e.DuplicateCount).ToComma()} exceptions) for '{Search}'{(SelectedLog.HasValue() ? " in " + SelectedLog : "")}";
                }
                if (Exception == null)
                {
                    if (SelectedGroup.HasValue() && SelectedLog.IsNullOrEmpty() && SelectedGroup != "All")
                    {
                        return TotalCount.Pluralize((SelectedGroup + " Exception").Trim());
                    }
                    return TotalCount.Pluralize((SelectedLog + " Exception").Trim());
                }
                return $"Most recent {Errors.Count} similar entries ({Errors.Sum(e => e.DuplicateCount).ToComma()} exceptions) from {SelectedLog}";
            }
        }

        public Dictionary<string, string> SearchDictionary
        {
            get
            {
                if (SelectedGroup.IsNullOrEmpty() && SelectedLog.IsNullOrEmpty()) return null;

                var result = new Dictionary<string, string>();
                if (SelectedGroup.HasValue()) result["group"] = SelectedGroup;
                if (SelectedLog.HasValue()) result["log"] = SelectedLog;
                return result;
            }
        }
    }
}