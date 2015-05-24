using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyAdmin
    {
        public const string AllServersKey = "*";
        private static readonly ParallelOptions _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3 };

        public static bool PerformProxyAction(IEnumerable<Proxy> proxies, string serverName, Action action)
        {
            var result = true;
            var matchingServers = proxies.SelectMany(p => p.Servers.Where(s => s.Name == serverName || serverName.IsNullOrEmpty()).Select(s => new { Proxy = p, Server = s }));
            Parallel.ForEach(matchingServers, _parallelOptions, pair =>
            {
                // HAProxy will not drain a downed server, do the next best thing: MAINT
                if (action == Action.drain && pair.Server.ProxyServerStatus == ProxyServerStatus.Down)
                    action = Action.maint;

                result = result && PostAction(pair.Proxy, pair.Server, action);
            });
            return result;
        }

        public static bool PerformProxyAction(Proxy proxy, Server server, Action action)
        {
            if (server != null)
            {
                var result = true;
                Parallel.ForEach(proxy.Servers, _parallelOptions, s =>
                    {
                        result = result && PostAction(proxy, s, action);
                    });
                return result;
            }
            else
            {
                return PostAction(proxy, server, action);
            }
        }

        public static bool PerformServerAction(string server, Action action)
        {
            var proxies = HAProxyGroup.GetAllProxies();
            var matchingServers = proxies.SelectMany(p => p.Servers.Where(s => s.Name == server).Select(s => new { Proxy = p, Server = s }));

            var result = true;
            Parallel.ForEach(matchingServers, _parallelOptions, pair =>
                {
                    result = result && PostAction(pair.Proxy, pair.Server, action);
                });
            return result;
        }

        public static bool PerformGroupAction(string group, Action action)
        {
            var haGroup = HAProxyGroup.GetGroup(group);
            if (haGroup == null) return false;
            var proxies = haGroup.GetProxies();
            var matchingServers = proxies.SelectMany(p => p.Servers.Select(s => new { Proxy = p, Server = s }));

            var result = true;
            Parallel.ForEach(matchingServers, _parallelOptions, pair =>
            {
                result = result && PostAction(pair.Proxy, pair.Server, action);
            });
            return result;
        }

        private static bool PostAction(Proxy p, Server server, Action action)
        {
            var instance = p.Instance;
            // if we can't issue any commands, bomb out
            if (instance.AdminUser.IsNullOrEmpty() || instance.AdminPassword.IsNullOrEmpty()) return false;

            var loginInfo = instance.AdminUser + ":" + instance.AdminPassword;
            var haproxyUri = new Uri(instance.Url);
            var requestBody = string.Format("s={0}&action={1}&b={2}", server.Name, action.ToString().ToLower(), p.Name);
            var requestHeader = string.Format("POST {0} HTTP/1.1\r\nHost: {1}\r\nContent-Length: {2}\r\nAuthorization: Basic {3}\r\n\r\n",
                haproxyUri.AbsolutePath, haproxyUri.Host, Encoding.GetEncoding("ISO-8859-1").GetBytes(requestBody).Length, Convert.ToBase64String(Encoding.Default.GetBytes(loginInfo)));

            try
            {
                var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(haproxyUri.Host, haproxyUri.Port);
                socket.Send(Encoding.UTF8.GetBytes(requestHeader + requestBody));

                var responseBytes = new byte[socket.ReceiveBufferSize];
                socket.Receive(responseBytes);

                var response = Encoding.UTF8.GetString(responseBytes);
                instance.PurgeCache();
                return response.StartsWith("HTTP/1.0 303") || response.StartsWith("HTTP/1.1 303");
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return false;
            }
        }

        public enum Action
        {
            [Description("Set State to READY")]
            ready,
            [Description("Set State to DRAIN")]
            drain,
            [Description("Set State to MAINT")]
            maint,
            [Description("Health: disable checks")]
            dhlth,
            [Description("Health: enable checks")]
            ehlth,
            [Description("Health: force UP")]
            hrunn,
            [Description("Health: force NOLB")]
            hnolb,
            [Description("Health: force DOWN")]
            hdown,
            [Description("Agent: disable checks")]
            dagent,
            [Description("Agent: enable checks")]
            eagent,
            [Description("Agent: force UP")]
            arunn,
            [Description("Agent: force DOWN")]
            adown,
            [Description("Kill Sessions")]
            shutdown,

            Enable,
            Disable
        }
    }
}