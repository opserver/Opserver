using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.CloudFlare
{
    public partial class RailgunInstance
    {
        public static List<RailgunInstance> AllInstances => _railgunInstances ?? (_railgunInstances = LoadRailgunInfos());

        private static readonly object _loadLock = new object();
        private static List<RailgunInstance> _railgunInstances;
        private static List<RailgunInstance> LoadRailgunInfos()
        {
            lock (_loadLock)
            {
                if (_railgunInstances != null) return _railgunInstances;
                _railgunInstances = new List<RailgunInstance>();

                if (Current.Settings.CloudFlare.Enabled)
                {
                    Current.Settings.CloudFlare.Railguns.ForEach(rg => _railgunInstances.Add(new RailgunInstance(rg)));

                    _railgunInstances = _railgunInstances.Where(rsi => rsi.TryAddToGlobalPollers()).ToList();
                }

                return _railgunInstances;
            }
        }

        public static RailgunInstance GetInstance(string connectionString)
        {
            if (connectionString.IsNullOrEmpty()) return null;
            if (connectionString.Contains(":"))
            {
                var parts = connectionString.Split(StringSplits.Colon);
                if (parts.Length != 2) return null;
                int port;
                if (int.TryParse(parts[1], out port)) return GetInstance(parts[0], port);
            }
            else
            {
                return GetInstances(connectionString).FirstOrDefault();
            }
            return null;
        }
        public static RailgunInstance GetInstance(string host, int port)
        {
            return AllInstances.FirstOrDefault(ri => ri.Host == host && ri.Port == port)
                ?? AllInstances.FirstOrDefault(ri => ri.Host.Split(StringSplits.Period).First() == host.Split(StringSplits.Period).First() && ri.Port == port);
        }

        public static List<RailgunInstance> GetInstances(string node)
        {
            var infos = AllInstances.Where(ri => string.Equals(ri.Host, node, StringComparison.InvariantCultureIgnoreCase));
            return infos.ToList();
        }

        public static bool IsRedisServer(string node)
        {
            return AllInstances.Any(i => string.Equals(i.Host, node, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
