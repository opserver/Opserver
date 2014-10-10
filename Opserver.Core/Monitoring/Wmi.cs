using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Management;

namespace StackExchange.Opserver.Monitoring
{
    internal static class Wmi
    {
        internal static WmiQuery Query(string machineName, string query)
        {
            return new WmiQuery(machineName, query);
        }

        private static ConnectionOptions GetConnectOptions(string machineName)
        {
            var co = new ConnectionOptions();
            if (machineName == Environment.MachineName)
                return co;

            switch (machineName)
            {
                case "localhost":
                case "127.0.0.1":
                case "::1":
                    return co;
                default:
                    co = new ConnectionOptions
                    {
                        Authentication = AuthenticationLevel.Packet,
                        Timeout = new TimeSpan(0, 0, 30),
                        EnablePrivileges = true
                    };
                    break;
            }
            var wps = Current.Settings.Polling.Windows;
            if (wps != null && wps.AuthUser.HasValue() && wps.AuthPassword.HasValue())
            {
                co.Username = wps.AuthUser;
                co.Password = wps.AuthPassword;
            }
            return co;
        }

        internal class WmiQuery : IDisposable
        {
            ManagementObjectCollection _data;
            ManagementObjectSearcher _searcher;

            public WmiQuery(string machineName, string q)
            {
                if (string.IsNullOrEmpty(machineName))
                    throw new ArgumentException("machineName should not be empty.");

                var connectionOptions = GetConnectOptions(machineName);
                var scope = new ManagementScope(string.Format(@"\\{0}\root\cimv2", machineName), connectionOptions);
                _searcher = new ManagementObjectSearcher(scope, new ObjectQuery(q), new EnumerationOptions{Timeout = connectionOptions.Timeout});
            }

            public ManagementObjectCollection Result
            {
                get
                {
                    if (_searcher == null)
                    {
                        throw new InvalidOperationException("Attempt to use disposed query.");
                    }
                    return _data ?? (_data = _searcher.Get());
                }
            }

            public IEnumerable<dynamic> GetDynamicResult()
            {
                return Result.Cast<ManagementObject>().Select(mo => new WmiDynamic(mo));
            }

            public dynamic GetFirstResult()
            {
                var obj = Result.Cast<ManagementObject>().FirstOrDefault();
                return obj == null ? null : new WmiDynamic(obj);
            }

            public void Dispose()
            {
                if (_data != null)
                {
                    _data.Dispose();
                    _data = null;
                }
                if (_searcher != null)
                {
                    _searcher.Dispose();
                    _searcher = null;
                }
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
