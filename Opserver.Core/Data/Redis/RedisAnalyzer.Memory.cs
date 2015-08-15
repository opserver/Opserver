using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Redis;

namespace StackExchange.Opserver.Data.Redis
{
    public class RedisAnalyzer
    {
        internal static readonly Dictionary<RedisConnectionInfo, List<KeyMatcher>> KeyMatchers;
        static RedisAnalyzer()
        {
            KeyMatchers = new Dictionary<RedisConnectionInfo, List<KeyMatcher>>();
            foreach (var i in RedisInstance.AllInstances.Select(rci => rci.ConnectionInfo))
            {
                KeyMatchers[i] = i.Settings.AnalysisRegexes
                                  .Where(r => r.Value.HasValue())
                                  .Select(r => new KeyMatcher {Name = r.Key, Regex = new Regex(r.Value, RegexOptions.Compiled)})
                                  .ToList();
                KeyMatchers[i].Add(new KeyMatcher {Name = "Other (unrecognized)", Regex = new Regex(".", RegexOptions.Compiled)});
            }
        }

        public static void AnalyzerInstanceMemory(RedisConnectionInfo connectionInfo)
        {
            //TODO: Get databases, suck in cache for each
        }

        private static string GetMemoryAnalysisKey(RedisConnectionInfo connectionInfo, int database)
        {
            return $"redis-memory-analysis-{connectionInfo.Host}:{connectionInfo.Port}:{database}";
        }

        public static RedisMemoryAnalysis AnalyzeDatabaseMemory(RedisConnectionInfo connectionInfo, int database)
        {
            using (MiniProfiler.Current.Step("Redis Memory Analysis for " + connectionInfo + " - DB:" + database))
            {
                return Current.LocalCache.GetSet<RedisMemoryAnalysis>(GetMemoryAnalysisKey(connectionInfo, database), (old, ctx) => GetDatabaseMemoryAnalysis(connectionInfo, database), 24 * 60 * 60, 24 * 60 * 60);
            }
        }

        public static void ClearDatabaseMemoryAnalysisCache(RedisConnectionInfo connectionInfo, int database)
        {
            Current.LocalCache.Remove(GetMemoryAnalysisKey(connectionInfo, database));
        }

        private static RedisMemoryAnalysis GetDatabaseMemoryAnalysis(RedisConnectionInfo connectionInfo, int database)
        {
            var config = new ConfigurationOptions
            {
                SyncTimeout = 10 * 60 * 1000,
                AllowAdmin = true,
                ClientName = "Status-MemoryAnalyzer",
                Password = connectionInfo.Password,
                EndPoints =
                {
                    { connectionInfo.Host, connectionInfo.Port }
                }
            };
            using (var muxer = ConnectionMultiplexer.Connect(config))
            {
                var ma = new RedisMemoryAnalysis(connectionInfo, database);
                if (ma.ErrorMessage.HasValue())
                {
                    return ma;
                }
                // Prep the match dictionary
                foreach (var km in KeyMatchers[connectionInfo])
                {
                    ma.KeyStats[km] = new KeyStats();
                }

                ma.Analyze(muxer);

                return ma;
            }
        }
    }

    public class RedisMemoryAnalysis : IMonitorStatus
    {
        public RedisConnectionInfo ConnectionInfo { get; internal set; }
        public bool IsGlobal => Database == -1;
        public int Database { get; internal set; }
        public DateTime CreationDate { get; internal set; }

        public TimeSpan KeyTime { get; internal set; }
        public TimeSpan AnalysisTime { get; internal set; }
        public TimeSpan TotalTime => KeyTime + AnalysisTime;

        public List<KeyMatcher> KeyMatchers { get; internal set; }
        public ConcurrentDictionary<KeyMatcher,KeyStats> KeyStats { get; internal set; }

        public IEnumerable<TopKey> TopKeys
        {
            get
            {
                return KeyStats.SelectMany(ks => ks.Value.TopKeys.Select(tk => new TopKey {Name = tk.Value, ValueBytes = tk.Key, Matcher = ks.Key }))
                               .OrderByDescending(tk => tk.TotalBytes)
                               .Take(50);
            }
        }  

        public MonitorStatus MonitorStatus
        {
            get
            {
                if (ErrorMessage.HasValue())
                    return MonitorStatus.Critical;
                if (ErrorCount > 0 && Count == 0)
                    return MonitorStatus.Critical;
                if (ErrorCount > 0 && Count > 0)
                    return MonitorStatus.Warning;
                return MonitorStatus.Good;
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                if (ErrorMessage.HasValue())
                    return ErrorMessage;
                if (ErrorCount > 0 && Count == 0)
                    return "Error processing all " + ErrorCount.ToComma() + " keys";
                if (ErrorCount > 0 && Count > 0)
                    return "Error processing " + ErrorCount.ToComma() + " of " + (ErrorCount + Count).ToComma() + " keys";
                return null;
            }
        }

        private long _count;
        private long _keyByteSize;
        private long _valueByteSize;
        private long _errorCount;

        public long Count => _count;
        public long KeyByteSize => _keyByteSize;
        public long ValueByteSize => _valueByteSize;
        public long TotalByteSize => _keyByteSize + _valueByteSize;
        public long ErrorCount => _errorCount;

        public string ErrorMessage { get; internal set; }

        public void Analyze(ConnectionMultiplexer muxer)
        {
            // Get the keys
            var sw = Stopwatch.StartNew();

            var db = muxer.GetDatabase(Database);
            var server = muxer.GetSingleServer();
            var keys = server.Keys(Database, pageSize: 1000);
            KeyTime = sw.Elapsed;

            muxer.PreserveAsyncOrder = true;
            // Analyze each key
            sw.Restart();
            using (MiniProfiler.Current.Step("Key analysis"))
            {
                Task last = null;
                foreach (var tmpKey in keys)
                {
                    var key = tmpKey;
                    last = db.DebugObjectAsync(key).ContinueWith(x =>
                        {
                            try
                            {
                                TallyDebugLine(key, x.Result);
                            }
                            catch (Exception e)
                            {
                                TallyError(e);
                            }
                        });
                }
                if (last != null) server.Wait(last);
            }
            AnalysisTime = sw.Elapsed;
            sw.Stop();
        }
        
        public RedisMemoryAnalysis(RedisConnectionInfo connectionInfo, int database)
        {
            CreationDate = DateTime.UtcNow;
            KeyStats = new ConcurrentDictionary<KeyMatcher, KeyStats>();

            ConnectionInfo = connectionInfo;
            Database = database;
            List<KeyMatcher> matchers;
            if (!RedisAnalyzer.KeyMatchers.TryGetValue(connectionInfo, out matchers))
            {
                ErrorMessage = "Could not find regexes defined for " + connectionInfo;
                return;
            }
            KeyMatchers = matchers;
            foreach (var km in matchers)
            {
                KeyStats[km] = new KeyStats();
            }
        }

        private static readonly Regex _debugObjectSize = new Regex(@"\bserializedlength:([0-9]+)\b", RegexOptions.Compiled);

        internal void TallyDebugLine(string key, string debugLine)
        {
            if (debugLine == null) return;

            var match = _debugObjectSize.Match(debugLine);
            long size;
            if (!match.Success || !long.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out size)) return;

            var matcher = GetKeyMatcher(key);
            var keySize = Encoding.UTF8.GetByteCount(key);
            // Global counts
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _keyByteSize, keySize);
            Interlocked.Add(ref _valueByteSize, size);

            if (matcher == null) return;

            // Per-key counts
            KeyStats[matcher].Tally(key, keySize, size);
        }

        private KeyMatcher GetKeyMatcher(string key)
        {
            for (int i = 0; i < KeyMatchers.Count; i++)
                if (KeyMatchers[i].Regex.IsMatch(key)) return KeyMatchers[i];
            return null;
        }

        internal void TallyError(Exception e)
        {
            Interlocked.Increment(ref _errorCount);
        }
    }

    public class TopKey
    {
        public string Name { get; internal set; }
        public KeyMatcher Matcher { get; internal set; }
        public int KeyBytes => Encoding.UTF8.GetByteCount(Name);
        public long ValueBytes { get; internal set; }
        public long TotalBytes => KeyBytes + ValueBytes;
    }

    public class KeyMatcher
    {
        public string Name { get; internal set; }
        public Regex Regex { get; internal set; }
    }

    public class KeyStats
    {
        private readonly object _lock = new object();

        internal long _count;
        internal long _keyByteSize;
        internal long _valueByteSize;

        public long Count => _count;
        public long KeyByteSize => _keyByteSize;
        public long ValueByteSize => _valueByteSize;
        public long TotalByteSize => _keyByteSize + _valueByteSize;

        public SortedList<long, string> TopKeys = new SortedList<long, string>(50, new DescLongCompare());

        public void Tally(string key, long keySize, long valueSize)
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _keyByteSize, keySize);
            Interlocked.Add(ref _valueByteSize, valueSize);

            lock (_lock) TopKeys.Add(valueSize, key);
        }

        class DescLongCompare : IComparer<long>
        {
            public int Compare(long x, long y)
            {
                return y.CompareTo(x);
            }
        }
    }
}
