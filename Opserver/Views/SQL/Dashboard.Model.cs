using System.Collections.Generic;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public enum SQLViews
    {
        Servers = 0,
        Jobs = 1,
        Instance = 3,
        Active = 4,
        Top = 5,
        Connections = 6,
        Databases = 7
    }

    public class DashboardModel
    {
        public SQLInstance CurrentInstance { get; set; }
        public string ErrorMessage { get; set; }

        public int Refresh { get; set; }
        public SQLViews View { get; set; }

        public enum LastRunInterval
        {
            FiveMinutes = 5 * 60,
            Hour = 60 * 60,
            Day = 24 * 60 * 60,
            Week = 7 * 24 * 60 * 60
        }

        public List<SQLInstance.SQLConnectionInfo> Connections { get; set; }
        public Cache Cache { get; set; }
    }
}