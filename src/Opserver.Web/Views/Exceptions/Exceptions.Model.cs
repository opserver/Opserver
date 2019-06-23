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

        public ExceptionsModule Module { get; internal set; }
        public ExceptionStore Store { get; internal set; }
        public List<ApplicationGroup> Groups { get; set; }
        public ApplicationGroup Group { get; set; }
        public Application Log { get; set; }
        public ExceptionSorts Sort { get; set; }

        public string Search => SearchParams?.SearchQuery;
        public ExceptionStore.SearchParams SearchParams { get; set; }
        public Error Exception { get; set; }
        public List<Error> Errors { get; set; }

        public bool ShowAll => Group == null && Log == null;
        private int? _shownCount;
        public int ShownCount => _shownCount ?? (_shownCount = Errors.Sum(e => e.DuplicateCount)).Value;
        public bool ShowClearLink => Current.User.IsExceptionAdmin && Errors.Any(e => !e.IsProtected) && (Log != null || SearchParams.SearchQuery.HasValue() || SearchParams.SearchQuery.HasValue());
        public bool ShowDeleted { get; set; }

        public int LoadAsyncSize { get; set; }

        private int? _totalCount;
        public int TotalCount => _totalCount ?? (_totalCount = GetTotal()).Value;
        private int GetTotal() => Log?.ExceptionCount ?? Group?.Total ?? Store?.TotalExceptionCount ?? Module.TotalExceptionCount;

        public bool HasMore => TotalCount > ShownCount;
        public string Title
        {
            get
            {
                if (Search.HasValue())
                {
                    return $"Showing search results for '{Search}'{(Log != null ? " in " + Log.Name : "")}";
                }
                if (SearchParams.Message.HasValue())
                {
                    return $"Most recent similar entries ({Errors.Sum(e => e.DuplicateCount).ToComma()} exceptions) from {Log?.Name}";
                }

                if (Log != null)
                {
                    return Log.ExceptionCount.Pluralize(Log.Name + " Exception");
                }
                else if (Group != null)
                {
                    return Group.Total.Pluralize(Group.Name + " Exception");
                }
                else if (Store != null)
                {
                    return Store.TotalExceptionCount.Pluralize(Store.Name + " Exception");
                }
                else
                {
                    return TotalCount.Pluralize("Exception");
                }
            }
        }

        public Dictionary<string, string> SearchDictionary
        {
            get
            {
                if (Store == null && Group == null && Log == null) return null;

                var result = new Dictionary<string, string>();
                if (Store != null) result["store"] = Store.Name;
                if (Group != null) result["group"] = Group.Name;
                if (Log != null) result["log"] = Log.Name;
                return result;
            }
        }
    }
}
