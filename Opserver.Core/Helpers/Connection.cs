﻿using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using MySql.Data.MySqlClient;

namespace StackExchange.Opserver.Helpers
{
    public static class Connection
    {
        /// <summary>
        /// Gets an open READ UNCOMMITTED connection using the specified connection string, optionally timing out on the initial connect
        /// </summary>
        /// <param name="connectionString">The connection string to use for the connection</param>
        /// <param name="connectionTimeout">Milliseconds to wait to connect, optional</param>
        /// <returns>A READ UNCOMMITTED connection to the specified connection string</returns>
        public static DbConnection GetOpen(string connectionString, int? connectionTimeout = null,string databasetype="mssql")
        {
            ProfiledDbConnection conn = null;
            if (databasetype=="mysql")
            {
               conn= new ProfiledDbConnection(new MySqlConnection(connectionString), MiniProfiler.Current);
            }
            else
            {
               conn= new ProfiledDbConnection(new SqlConnection(connectionString), MiniProfiler.Current);
            }
          
            Action<DbConnection> setReadUncommitted = c =>
                {
                    //TODO
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
                        cmd.ExecuteNonQueryAsync();
                    }
                };

            if (connectionTimeout.GetValueOrDefault(0) == 0)
            {
                conn.OpenAsync();
                setReadUncommitted(conn);
            }
            else
            {
                // In the case of remote monitoring, the timeout will be at the NIC level, not responding to traffic,
                // in that scenario, connection timeouts don't really do much, because they're never reached, the timeout happens
                // before their timer starts.  Because of that, we need to spin up our own overall timeout
                using (MiniProfiler.Current.Step($"Opening Connection, Timeout: {conn.ConnectionTimeout.ToString()}"))
                try
                {
                    conn.Open();
                }//TODO
                catch (SqlException e)
                {
                    var csb = new SqlConnectionStringBuilder(connectionString);
                    var sqlException = $"Error opening connection to {csb.InitialCatalog} at {csb.DataSource} timeout was: {connectionTimeout.ToComma()} ms";
                    throw new Exception(sqlException, e)
                            .AddLoggedData("Timeout", conn.ConnectionTimeout.ToString());
                }
                setReadUncommitted(conn);
                if (conn.State == ConnectionState.Connecting)
                {
                    var b = new SqlConnectionStringBuilder { ConnectionString = connectionString };

                    throw new TimeoutException($"Timeout expired connecting to {b.InitialCatalog} on {b.DataSource} on in the alloted {connectionTimeout.ToComma()} ms");
                }
            }
            return conn;
        }

        /// <summary>
        /// Gets an open READ UNCOMMITTED connection using the specified connection string, optionally timing out on the initial connect
        /// </summary>
        /// <param name="connectionString">The connection string to use for the connection</param>
        /// <param name="connectionTimeout">Milliseconds to wait to connect, optional</param>
        /// <returns>A READ UNCOMMITTED connection to the specified connection string</returns>
        public static async Task<DbConnection> GetOpenAsync(string connectionString, int? connectionTimeout = null,string databasetype="mssql")
        {
            ProfiledDbConnection conn = null;
            if (databasetype == "mysql")
            {
                conn = new ProfiledDbConnection(new MySqlConnection(connectionString), MiniProfiler.Current);
            }
            else
            {
                conn = new ProfiledDbConnection(new SqlConnection(connectionString), MiniProfiler.Current);
            }
            if (connectionTimeout.GetValueOrDefault(0) == 0)
            {
                await conn.OpenAsync().ConfigureAwait(false);
                await conn.SetReadUncommittedAsync().ConfigureAwait(false);
            }
            else
            {
                // In the case of remote monitoring, the timeout will be at the NIC level, not responding to traffic,
                // in that scenario, connection timeouts don't really do much, because they're never reached, the timeout happens
                // before their timer starts.  Because of that, we need to spin up our own overall timeout
                using (MiniProfiler.Current.Step($"Opening Connection, Timeout: {conn.ConnectionTimeout.ToString()}"))
                using (var tokenSource = new CancellationTokenSource())
                {
                    tokenSource.CancelAfter(connectionTimeout.Value);
                    try
                    {
                        await conn.OpenAsync(tokenSource.Token).ConfigureAwait(false); // Throwing Null Refs
                        await conn.SetReadUncommittedAsync().ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        conn.Close();
                        var csb = new SqlConnectionStringBuilder(connectionString);
                        var sqlException = $"Error opening connection to {csb.InitialCatalog} at {csb.DataSource}, timeout out at {connectionTimeout.ToComma()} ms";
                        throw new Exception(sqlException, e);
                    }//TODO
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
                        throw new TimeoutException($"Timeout expired connecting to {b.InitialCatalog} on {b.DataSource} on in the alloted {connectionTimeout.Value.ToComma()} ms");
                    }
                }
            }
            return conn;
        }
    }
}