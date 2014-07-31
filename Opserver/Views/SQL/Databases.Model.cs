using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class DatabasesModel
    {
        public SQLInstance Instance { get; set; }
        public string Database { get; set; }
        public string ObjectName { get; set; }
        public Views View { get; set; }

        // TODO: Remove for extensibility
        public enum Views
        {
            Tables,
            Views,
            BlitzIndex,
            MissingIndexes,
            UnusedIndexes,
            Storage,
            Other
        }

        public static string GetDatabaseClass(SQLInstance.SQLDatabaseInfo db)
        {
            if (db.IsSystemDatabase) return "system";
            if (db.State == DatabaseStates.Restoring) return "restoring";

            return db.MonitorStatus.Class();
        }
    }
}