using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Monitoring
{
    internal static class Wmi
    {
        private const string defaultWmiNamespace = @"root\cimv2";

        internal static WmiQuery Query(string machineName, string query, string wmiNamespace = defaultWmiNamespace)
        {
            return new WmiQuery(machineName, query, wmiNamespace);
        }

        internal static async Task<bool> ClassExists(string machineName, string @class, string wmiNamespace = defaultWmiNamespace)
        {
            // it's much faster trying to query something potentially non existent and catching an exception than to query the "meta_class" table.
            var query = $"SELECT * FROM {@class}";

            try
            {
                using (var q = Query(machineName, query, wmiNamespace))
                {
                    await q.GetFirstResultAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static readonly ConnectionOptions _localOptions, _remoteOptions;

        static Wmi()
        {
            _localOptions = new ConnectionOptions
            {
                EnablePrivileges = true
            };
            _remoteOptions = new ConnectionOptions
            {
                EnablePrivileges = true,
                Authentication = AuthenticationLevel.Packet,
                Timeout = TimeSpan.FromSeconds(30)
            };
            string username = Current.Settings.Dashboard.Providers?.WMI?.Username ??
                              Current.Settings.Polling.Windows?.AuthUser.IsNullOrEmptyReturn(null),
                password = Current.Settings.Dashboard.Providers?.WMI?.Password ??
                           Current.Settings.Polling.Windows?.AuthPassword.IsNullOrEmptyReturn(null);

            if (username.HasValue() && password.HasValue())
            {
                _remoteOptions.Username = username;
                _remoteOptions.Password = password;
            }
        }

        private static ConnectionOptions GetConnectOptions(string machineName)
        {
            if (machineName == Environment.MachineName)
                return _localOptions;

            switch (machineName)
            {
                case "localhost":
                case "127.0.0.1":
                case "::1":
                    return _localOptions;
                default:
                    return _remoteOptions;
            }
        }

        internal class WmiQuery : IDisposable
        {
            private static readonly ConcurrentDictionary<string, ManagementScope> _scopeCache = new ConcurrentDictionary<string, ManagementScope>();
            private static readonly ConcurrentDictionary<string, ManagementObjectSearcher> _searcherCache = new ConcurrentDictionary<string, ManagementObjectSearcher>();

            private ManagementObjectCollection _data;
            private readonly ManagementObjectSearcher _searcher;
            private readonly string _machineName;
            private readonly string _rawQuery;

            public WmiQuery(string machineName, string q, string wmiNamespace = @"root\cimv2")
            {
                _machineName = machineName;
                _rawQuery = q;
                if (machineName.IsNullOrEmpty())
                    throw new ArgumentException("machineName should not be empty.");

                var connectionOptions = GetConnectOptions(machineName);

                var path = $@"\\{machineName}\{wmiNamespace}";
                var scope = _scopeCache.GetOrAdd(path, x => new ManagementScope(x, connectionOptions));
                _searcher = _searcherCache.GetOrAdd(path + q, x => new ManagementObjectSearcher(scope, new ObjectQuery(q), new EnumerationOptions { Timeout = connectionOptions.Timeout }));
            }

            public Task<ManagementObjectCollection> Result
            {
                get
                {
                    if (_searcher == null)
                    {
                        throw new InvalidOperationException("Attempt to use disposed query.");
                    }

                    return _data != null ? Task.FromResult(_data) : Task.Run(() => _data = _searcher.Get());
                }
            }

            public async Task<IEnumerable<dynamic>> GetDynamicResultAsync()
            {
                using (MiniProfiler.Current.CustomTiming("WMI", _rawQuery, _machineName))
                    return (await Result.ConfigureAwait(false)).Cast<ManagementObject>().Select(mo => new WmiDynamic(mo));
            }

            public async Task<dynamic> GetFirstResultAsync()
            {
                ManagementObject obj;
                using (MiniProfiler.Current.CustomTiming("WMI", _rawQuery, _machineName))
                    obj = (await Result.ConfigureAwait(false)).Cast<ManagementObject>().FirstOrDefault();
                return obj == null ? null : new WmiDynamic(obj);
            }

            public void Dispose()
            {
                _data?.Dispose();
                _data = null;
            }
        }

        private class WmiDynamic : DynamicObject
        {
            private readonly ManagementObject _obj;
            public WmiDynamic(ManagementObject obj)
            {
                _obj = obj;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                if (binder.Type == typeof(ManagementObject))
                {
                    result = _obj;
                    return true;
                }

                return base.TryConvert(binder, out result);
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                try
                {
                    result = _obj.Properties[binder.Name].Value;
                    return true;
                }
                catch (ManagementException)
                {
                    result = null;
                    return false;
                }
            }

            public override IEnumerable<string> GetDynamicMemberNames()
                => _obj.Properties.Cast<PropertyData>().Select(x => x.Name);
        }
    }
}