using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public enum SQLViews
    {
        Servers,
        Instance,
        Active,
        Top,
        Connections,
        Databases
    }

    public class DashboardModel
    {
        public SQLInstance CurrentInstance { get; set; }
        public string ErrorMessage { get; set; }

        public int Refresh { get; set; }
        public SQLViews View { get; set; }

        public enum LastRunInterval
        {
            Week = 7*24*60*60,
            Day = 24*60*60,
            Hour = 60*60,
            FiveMinutes = 5*60
        }
    }
}