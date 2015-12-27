namespace StackExchange.Opserver.Helpers
{
    /// <summary>
    /// Contains keys for common app-wide keys (used in querystring, etc)
    /// </summary>
    public static class Keys
    {
        public const string ReturnUrl = "returnurl";
    }

    /// <summary>
    /// Contains keys for common ViewData collection values
    /// </summary>
    public static class ViewDataKeys
    {
        public const string PageTitle = nameof(PageTitle);
        public const string HeaderSubtitle = nameof(HeaderSubtitle);
        public const string MainTab = "Tab";
        public const string Error = nameof(Error);
        public const string QueryString = nameof(QueryString);
        public const string TopBoxOptions = nameof(TopBoxOptions);
    }
}