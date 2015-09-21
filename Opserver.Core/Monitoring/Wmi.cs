using System;
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
        internal static WmiQuery Query(string machineName, string query, string wmiNamespace = @"root\cimv2")
        {
            return new WmiQuery(machineName, query, wmiNamespace);
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
            ManagementObjectCollection _data;
            ManagementObjectSearcher _searcher;
            private readonly string _machineName;
            private readonly string _rawQuery;

            public WmiQuery(string machineName, string q, string wmiNamespace = @"root\cimv2")
            {
                _machineName = machineName;
                _rawQuery = q;
                if (string.IsNullOrEmpty(machineName))
                    throw new ArgumentException("machineName should not be empty.");

                var connectionOptions = GetConnectOptions(machineName);
                var scope = new ManagementScope($@"\\{machineName}\{wmiNamespace}", connectionOptions);
                _searcher = new ManagementObjectSearcher(scope, new ObjectQuery(q), new EnumerationOptions{Timeout = connectionOptions.Timeout});
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

            public async Task<IEnumerable<dynamic>> GetDynamicResult()
            {
                using (MiniProfiler.Current.CustomTiming("WMI", _rawQuery, _machineName))
                    return (await Result).Cast<ManagementObject>().Select(mo => new WmiDynamic(mo));
            }

            public async Task<dynamic> GetFirstResult()
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
                _searcher = null;
            }
        }

        class WmiDynamic : DynamicObject
        {
            readonly ManagementObject _obj;
            public WmiDynamic(ManagementObject obj)
            {
                _obj = obj;
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
        }
    }
}
