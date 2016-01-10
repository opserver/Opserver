using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace StackExchange.Opserver.Monitoring
{
    public class LightweightProfiler : IDbProfiler
    {
        /// <summary>
        /// Can be a full <see cref="MiniProfiler"/>.
        /// </summary>
        private readonly IDbProfiler _wrapped;

        private readonly Stopwatch _sw = new Stopwatch();
        private readonly string _category; // future use for multiples
        public LightweightProfiler(IDbProfiler wrapped, string category)
        {
            _category = category;
            _wrapped = wrapped;
        }

        /// <summary>
        /// Number of statements executed on the request.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Aggregate time executing sql on the request.
        /// </summary>
        public long TotalMilliseconds => _sw.ElapsedMilliseconds;

        public void ExecuteStart(IDbCommand profiledDbCommand, SqlExecuteType executeType)
        {
            Count++;

            _wrapped?.ExecuteStart(profiledDbCommand, executeType);

            // This is subtle, avoid measure time gathering stack trace (that happens on execute start)
            _sw.Start();
        }

        public void ExecuteFinish(IDbCommand profiledDbCommand, SqlExecuteType executeType, DbDataReader reader)
        {
            if (reader == null)
            {
                _sw.Stop();
            }
            _wrapped?.ExecuteFinish(profiledDbCommand, executeType, reader);
        }

        public void ReaderFinish(IDataReader reader)
        {
            _sw.Stop();
            _wrapped?.ReaderFinish(reader);
        }

        public void OnError(IDbCommand profiledDbCommand, SqlExecuteType executeType, Exception e)
        {
            var formatter = new Profiling.SqlFormatters.SqlServerFormatter();
            var parameters = SqlTiming.GetCommandParameters(profiledDbCommand);
            e.Data["SQL"] = formatter.FormatSql(profiledDbCommand.CommandText, parameters);
        }

        public bool IsActive => true;
    }
}