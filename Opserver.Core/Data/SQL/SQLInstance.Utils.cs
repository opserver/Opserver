using System;
using Dapper;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        /// <summary>
        /// Removes a query plan from the cache
        /// </summary>
        public int RemovePlan(byte[] planHandle)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    return conn.Execute("DBCC FREEPROCCACHE (@planHandle);", new { planHandle });
                }
            }
            catch (Exception ex)
            {
                Current.LogException(ex);
                return 0;
            }
        }
    }
}
