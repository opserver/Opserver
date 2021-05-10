using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace Opserver.Helpers
{
    internal static class Wmi
    {
        public const string DefaultWmiNamespace = @"root\cimv2";

        internal class WmiQuery : IDisposable
        {
            private static readonly ConnectionOptions _localOptions = new ConnectionOptions
            {
                EnablePrivileges = true
            };
            private static readonly ConcurrentDictionary<(string Username, string Password), ConnectionOptions> _optionsCache = new ConcurrentDictionary<(string Username, string Password), ConnectionOptions>();
            private static readonly ConcurrentDictionary<string, ManagementScope> _scopeCache = new ConcurrentDictionary<string, ManagementScope>();
            private static readonly ConcurrentDictionary<string, ManagementObjectSearcher> _searcherCache = new ConcurrentDictionary<string, ManagementObjectSearcher>();

            private ManagementObjectCollection _data;
            private readonly ManagementObjectSearcher _searcher;
            private readonly string _machineName;
            private readonly string _rawQuery;
            private readonly string _wmiNamespace;

            public WmiQuery(WMISettings settings, string machineName, string query, string wmiNamespace = @"root\cimv2") :
                this(machineName, query, wmiNamespace, credentials: (settings.Username, settings.Password)) { }

            public WmiQuery(string machineName, string query, string wmiNamespace = @"root\cimv2", (string Username, string Password) credentials = default)
            {
                _machineName = machineName;
                _rawQuery = query;
                _wmiNamespace = wmiNamespace;
                if (machineName.IsNullOrEmpty())
                    throw new ArgumentException("machineName should not be empty.");

                var connectionOptions = GetConnectOptions(credentials, machineName);

                var path = $@"\\{machineName}\{wmiNamespace}";
                var scope = _scopeCache.GetOrAdd(path, x => new ManagementScope(x, connectionOptions));
                _searcher = _searcherCache.GetOrAdd(path + query, _ => new ManagementObjectSearcher(scope, new ObjectQuery(query), new EnumerationOptions { Timeout = connectionOptions.Timeout }));
            }

            private static ConnectionOptions GetConnectOptions((string Username, string Password) credentials, string machineName)
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
                        return _optionsCache.GetOrAdd(credentials, tuple =>
                        {
                            var options = new ConnectionOptions
                            {
                                EnablePrivileges = true,
                                Authentication = AuthenticationLevel.Packet,
                                Timeout = TimeSpan.FromSeconds(30)
                            };
                            if (tuple.Username.HasValue() && tuple.Password.HasValue())
                            {
                                options.Username = tuple.Username;
                                options.Password = tuple.Password;
                            }
                            return options;
                        });
                }
            }

            public Task<ManagementObjectCollection> Result
            {
                get
                {
                    if (_searcher == null)
                    {
                        throw new InvalidOperationException("Attempt to use disposed query.");
                    }

                    return _data != null ? Task.FromResult(_data) : Task.Run(() =>
                    {
                        try { return _data = _searcher.Get(); }
                        catch (Exception ex)
                        {
                            // Without this WMI queries will continue to fail after a machine reboots.
                            if (ex is System.Runtime.InteropServices.COMException)
                            {
                                foreach (var scopeCacheItem in _scopeCache)
                                {
                                    if (scopeCacheItem.Key.StartsWith($@"\\{_machineName}"))
                                        _scopeCache.TryRemove(scopeCacheItem.Key, out var scopeCacheValue);
                                }
                                foreach (var searchCacheItem in _searcherCache)
                                {
                                    if (searchCacheItem.Key.StartsWith($@"\\{_machineName}"))
                                        _searcherCache.TryRemove(searchCacheItem.Key, out var searchCacheValue);
                                }
                            }

                            throw new Exception($"Failed to query {_wmiNamespace} on {_machineName}", ex);
                        }
                    });
                }
            }

            public async Task<IEnumerable<dynamic>> GetDynamicResultAsync()
            {
                using (MiniProfiler.Current.CustomTiming("WMI", _rawQuery, _machineName))
                    return (await Result).Cast<ManagementObject>().Select(mo => new WmiDynamic(mo));
            }

            public async Task<dynamic> GetFirstResultAsync()
            {
                ManagementObject obj;
                using (MiniProfiler.Current.CustomTiming("WMI", _rawQuery, _machineName))
                    obj = (await Result).Cast<ManagementObject>().FirstOrDefault();
                return obj == null ? null : new WmiDynamic(obj);
            }

            public void Dispose()
            {
                _data?.Dispose();
                _data = null;
                _searcher?.Dispose();
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

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                try
                {
                    result = _obj.InvokeMethod(binder.Name, args);
                    return true;
                }
                catch
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
