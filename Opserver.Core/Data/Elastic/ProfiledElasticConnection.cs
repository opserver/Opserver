using Nest;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Elastic
{
    /// <summary>
    /// MiniProfiler wrapper for IConnection in NEST, this allows us to profile all elasticsearch usage through NEST
    /// while plugging into one place.
    /// Thanks to Matt Jibson for implementing both this and much of the MiniProfiler custom timings it works with.
    /// </summary>
    public class ProfiledElasticConnection : IConnection
    {
        private const string _timingName = "elastic";
        private readonly Connection _conn;
        private readonly IConnectionSettings _settings;

        public ProfiledElasticConnection(IConnectionSettings settings)
        {
            _settings = settings;
            _conn = new Connection(settings);
        }

        private String format(string path, string data = null)
        {
            return String.Format(data.HasValue() ? "{0}{1}\n\n{2}" : "{0}{1}", _settings.Uri, path, data);
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Delete(string path, string data)
        {
            return _conn.Delete(path, data);
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Delete(string path)
        {
            return _conn.Delete(path);
        }

        public ConnectionStatus DeleteSync(string path, string data)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path, data), "delete"))
            {
                return _conn.DeleteSync(path, data);
            }
        }

        public ConnectionStatus DeleteSync(string path)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path), "delete"))
            {
                return _conn.DeleteSync(path);
            }
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Get(string path)
        {
            return _conn.Get(path);
        }

        public ConnectionStatus GetSync(string path)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path), "get"))
            {
                return _conn.GetSync(path);
            }
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Head(string path)
        {
            return _conn.Head(path);
        }

        public ConnectionStatus HeadSync(string path)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path), "head"))
            {
                return _conn.HeadSync(path);
            }
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Post(string path, string data)
        {
            return _conn.Post(path, data);
        }

        public ConnectionStatus PostSync(string path, string data)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path, data), "post"))
            {
                return _conn.PostSync(path, data);
            }
        }

        public System.Threading.Tasks.Task<ConnectionStatus> Put(string path, string data)
        {
            return _conn.Put(path, data);
        }

        public ConnectionStatus PutSync(string path, string data)
        {
            using (MiniProfiler.Current.CustomTiming(_timingName, format(path, data), "put"))
            {
                return _conn.PutSync(path, data);
            }
        }
    }
}
