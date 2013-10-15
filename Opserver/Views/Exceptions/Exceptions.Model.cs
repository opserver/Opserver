using System.Collections.Generic;
using System.Linq;
using StackExchange.Exceptional;
using StackExchange.Opserver.Data.Exceptions;

namespace StackExchange.Opserver.Views.Exceptions
{
    public class ExceptionsModel
    {
        public string SelectedLog { get; set; }
        public bool TruncateErrors { get; set; }
        public bool ShowingWindow { get; set; }

        public string Search { get; set; }
        public Error Exception { get; set; }
        public List<Application> Applications { get; set; }
        public List<Error> Errors { get; set; }

        public bool ShowDeleted { get; set; }
        public bool ShowAll { get { return SelectedLog.IsNullOrEmpty(); } }
        public int TotalCount { get { return Applications.Where(a => ShowAll || a.Name == SelectedLog).Sum(a => a.ExceptionCount); } }
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