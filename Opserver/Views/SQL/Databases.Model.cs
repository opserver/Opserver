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
            Tables,
            Backups,
            Views,
            BlitzIndex,
            MissingIndexes,
            UnusedIndexes,
            Storage,
            Other,
            Restores,
            StoredProcedures
        }

        public static string GetDatabaseClass(SQLInstance.Database db)
        {
            if (db.IsSystemDatabase) return "text-primary";
            if (db.State == DatabaseStates.Restoring) return StatusIndicator.WarningClass;

            return db.MonitorStatus.TextClass(showGood: true);
        }
    }
}