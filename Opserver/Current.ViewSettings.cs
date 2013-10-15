namespace StackExchange.Opserver
{
    public partial class Current
    {
        public static class ViewSettings
        {
            public const int SummaryChartWidth = 500;
            public const int SummaryChartHeight = 250;

            public const int SparklineChartWidth = 200;
            public const int SparklineChartHeight = 20;

            /// <summary>
            /// How many days to show in the summary/home view of servers
            /// </summary>
            public const int SummaryDaysAgo = 1;

            /// <summary>
            /// How many days to show in the daily per-server view
            /// </summary>
            public const int DailyDaysAgo = 1;

            /// <summary>
            /// How many days to show in the weekly per-server view
            /// </summary>
            public const int WeeklyDaysAgo = 7;

            /// <summary>
            /// How many days to show in the monthly per-server view
            /// </summary>
            public const int MonthlyDaysAgo = 30;

            /// <summary>
            /// How many days to show in the yearly per-server view
            /// </summary>
            public const int YearlyDaysAgo = 365;
        }
    }
}