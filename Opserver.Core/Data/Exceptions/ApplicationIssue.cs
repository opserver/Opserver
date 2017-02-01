namespace StackExchange.Opserver.Data.Exceptions
{
    public class ApplicationIssue
    {
        public string Application { get; set; }
        public int IssueCount { get; set; }
        public int RecentIssueCount { get; set; }
    }
}
