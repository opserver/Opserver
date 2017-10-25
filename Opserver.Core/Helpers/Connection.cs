using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace StackExchange.Opserver.Helpers
{
    public static class Connection
    {
        /// <summary>
        /// Gets an open READ UNCOMMITTED connection using the specified connection string, optionally timing out on the initial connect.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the connection.</param>
        /// <param name="connectionTimeoutMs">(Optional) Milliseconds to wait to connect.</param>
        /// <returns>A READ UNCOMMITTED connection to the specified connection string.</returns>
        /// <exception cref="Exception">Throws if a connection isn't able to be made.</exception>
        /// <exception cref="TimeoutException">Throws if a connection can't be made in <paramref name="connectionTimeoutMs"/>.</exception>
        public static DbConnection GetOpen(string connectionString, int? connectionTimeoutMs = null)
        {
            var conn = new ProfiledDbConnection(new SqlConnection(connectionString), MiniProfiler.Current);
            void setReadUncommitted(DbConnection c)
            {
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
                    cmd.ExecuteNonQueryAsync();
                }
            }

            if (connectionTimeoutMs.GetValueOrDefault(0) == 0)
            {
                conn.OpenAsync();
                setReadUncommitted(conn);
            }
            else
            {
                // In the case of remote monitoring, the timeout will be at the NIC level, not responding to traffic,
                // in that scenario, connection timeouts don't really do much, because they're never reached, the timeout happens
                // before their timer starts.  Because of that, we need to spin up our own overall timeout
                using (MiniProfiler.Current.Step($"Opening Connection, Timeout: {conn.ConnectionTimeout}"))
                {
                    try
                    {
                        conn.Open();
                    }
                    catch (SqlException e)
                    {
                        var csb = new SqlConnectionStringBuilder(connectionString);
                        var sqlException = $"Error opening connection to {csb.InitialCatalog} at {csb.DataSource} timeout was: {connectionTimeoutMs.ToComma()} ms";
                        throw new Exception(sqlException, e)
                                .AddLoggedData("Timeout", conn.ConnectionTimeout.ToString());
                    }
                }

                setReadUncommitted(conn);
                if (conn.State == ConnectionState.Connecting)
                {
                    var b = new SqlConnectionStringBuilder { ConnectionString = connectionString };

                    throw new TimeoutException($"Timeout expired connecting to {b.InitialCatalog} on {b.DataSource} on in the alloted {connectionTimeoutMs.ToComma()} ms");
                }
            }
            return conn;
        }

        /// <summary>
        /// Gets an open READ UNCOMMITTED connection using the specified connection string, optionally timing out on the initial connect.
        /// </summary>
        /// <param name="connectionString">The connection string to use for the connection.</param>
        /// <param name="connectionTimeoutMs">(Optional) Milliseconds to wait to connect.</param>
        /// <returns>A READ UNCOMMITTED connection to the specified connection string.</returns>
        /// <exception cref="Exception">Throws if a connection isn't able to be made.</exception>
        /// <exception cref="TimeoutException">Throws if a connection can't be made in <paramref name="connectionTimeoutMs"/>.</exception>
        public static async Task<DbConnection> GetOpenAsync(string connectionString, int? connectionTimeoutMs = null)
        {
            var conn = new ProfiledDbConnection(new SqlConnection(connectionString), MiniProfiler.Current);

            if (connectionTimeoutMs.GetValueOrDefault(0) == 0)
            {
                await conn.OpenAsync().ConfigureAwait(false);
                await conn.SetReadUncommittedAsync().ConfigureAwait(false);
            }
            else
            {
                // In the case of remote monitoring, the timeout will be at the NIC level, not responding to traffic,
                // in that scenario, connection timeouts don't really do much, because they're never reached, the timeout happens
                // before their timer starts.  Because of that, we need to spin up our own overall timeout
                using (MiniProfiler.Current.Step($"Opening Connection, Timeout: {conn.ConnectionTimeout}"))
                using (var tokenSource = new CancellationTokenSource())
                {
                    tokenSource.CancelAfter(connectionTimeoutMs.Value);
                    try
                    {
                        await conn.OpenAsync(tokenSource.Token).ConfigureAwait(false); // Throwing Null Refs
                        await conn.SetReadUncommittedAsync().ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        conn.Close();
                        var csb = new SqlConnectionStringBuilder(connectionString);
                        var sqlException = $"Error opening connection to {csb.InitialCatalog} at {csb.DataSource}, timeout out at {connectionTimeoutMs.ToComma()} ms";
                        throw new Exception(sqlException, e);
                    }
                    catch (SqlException e)
                    {
                        conn.Close();
                        var csb = new SqlConnectionStringBuilder(connectionString);
                        var sqlException = $"Error opening connection to {csb.InitialCatalog} at {csb.DataSource}: {e.Message}";
                        throw new Exception(sqlException, e);
                    }
                    if (conn.State == ConnectionState.Connecting)
                    {
                        tokenSource.Cancel();
                        var b = new SqlConnectionStringBuilder {ConnectionString = connectionString};
                        throw new TimeoutException($"Timeout expired connecting to {b.InitialCatalog} on {b.DataSource} on in the alloted {connectionTimeoutMs.Value.ToComma()} ms");
                    }
                }
            }
            return conn;
        }
    }
}