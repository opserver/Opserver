using StackExchange.Opserver.Data.SQL;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Views.SQL
{
    public class DatabasesModel
    {
        public SQLInstance Instance { get; set; }
        public string Database { get; set; }
        public string ObjectName { get; set; }
        public Views View { get; set; }

        // TODO: Remove for extensibility, create a dictionary instead and nameof()
        public enum Views
        {
            Tables = 0,
            Backups = 1,
            Views = 2,
            BlitzIndex = 3,
            MissingIndexes = 4,
            UnusedIndexes = 5,
            Storage = 6,
            Other = 7,
            Restores = 8,
            StoredProcedures = 9
        }

        public static string GetDatabaseClass(SQLInstance.Database db)
        {
            if (db.IsSystemDatabase) return "text-primary";
            if (db.State == DatabaseStates.Restoring) return StatusIndicator.WarningClass;

            return db.MonitorStatus.TextClass(showGood: true);
        }
    }
}