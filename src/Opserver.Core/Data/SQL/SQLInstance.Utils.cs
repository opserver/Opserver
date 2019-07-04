using System;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLInstance
    {
        /// <summary>
        /// Removes a query plan from the cache
        /// </summary>
        /// <param name="planHandle">The handle of the plan to fetch</param>
        public async Task<int> RemovePlanAsync(byte[] planHandle)
        {
            try
            {
                using (var conn = await GetConnectionAsync().ConfigureAwait(false))
                {
                    return await conn.ExecuteAsync("DBCC FREEPROCCACHE (@planHandle);", new { planHandle }).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ex.Log();
                return 0;
            }
        }
    }
}
