using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnconstrainedMelody;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyAdmin
    {
        public const string AllServersKey = "*";
        
        private class ActionPair
        {
            public Proxy Proxy { get; set; }
            public Server Server { get; set; }
        }

        public static Task<bool> PerformProxyActionAsync(IEnumerable<Proxy> proxies, string serverName, Action action)
        {
            var pairs = proxies
                .SelectMany(p => p.Servers
                    .Where(s => s.Name == serverName || serverName.IsNullOrEmpty())
                    .Select(s => new ActionPair {Proxy = p, Server = s})
                ).ToList();

            return PostActionsAsync(pairs, action);
        }

        public static Task<bool> PerformServerActionAsync(string server, Action action)
        {
            var proxies = HAProxyGroup.GetAllProxies();
            var pairs = proxies
                .SelectMany(p => p.Servers
                    .Where(s => s.Name == server)
                    .Select(s => new ActionPair { Proxy = p, Server = s})
                ).ToList();

            return PostActionsAsync(pairs, action);
        }

        public static async Task<bool> PerformGroupActionAsync(string group, Action action)
        {
            var haGroup = HAProxyGroup.GetGroup(group);
            if (haGroup == null) return false;

            var pairs = haGroup.GetProxies()
                .SelectMany(p => p.Servers
                    .Select(s => new ActionPair {Proxy = p, Server = s})
                ).ToList();

            return await PostActionsAsync(pairs, action).ConfigureAwait(true);
        }

        private static async Task<bool> PostActionsAsync(List<ActionPair> pairs, Action action)
        {
            var instances = new HashSet<HAProxyInstance>();
            var tasks = new List<Task<bool>>(pairs.Count);
            foreach (var p in pairs)
            {
                tasks.Add(PostAction(p.Proxy, p.Server, action));
                instances.Add(p.Proxy.Instance);
            }
            var result = (await Task.WhenAll(tasks)).All(r => r);
            // Actions complete, now re-check status
            var instanceTasks = instances.Select(i => i.Proxies.PollAsync(true));
            await Task.WhenAll(instanceTasks);
            return result;
        }
        
        private static async Task<bool> PostAction(Proxy p, Server server, Action action)
        {
            var instance = p.Instance;
            // if we can't issue any commands, bomb out
            if (instance.AdminUser.IsNullOrEmpty() || instance.AdminPassword.IsNullOrEmpty())
            {
                return false;
            }

            // HAProxy will not drain a downed server, do the next best thing: MAINT
            if (action == Action.Drain && server.ProxyServerStatus == ProxyServerStatus.Down)
            {
                action = Action.Maintenance;
            }

            try
            {
                using (var wc = new WebClient())
                {
                    var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{instance.AdminUser}:{instance.AdminPassword}"));
                    wc.Headers[HttpRequestHeader.Authorization] = "Basic " + creds;

                    var responseBytes = await wc.UploadValuesTaskAsync(instance.Url, new NameValueCollection
                    {
                        ["s"] = server.Name,
                        ["action"] = action.GetDescription(),
                        ["b"] = p.Name
                    });
                    var response = Encoding.UTF8.GetString(responseBytes);
                    return response.StartsWith("HTTP/1.0 303") || response.StartsWith("HTTP/1.1 303");
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return false;
            }
        }
    }
    

    [Flags]
    public enum Action
    {
        // ReSharper disable InconsistentNaming
        None = 0,
        [Description("ready")] // Set State to READY
        Ready = 1 << 1,
        [Description("drain")] // Set State to DRAIN
        Drain = 1 << 2,
        [Description("maint")] // Set State to MAINT
        Maintenance = 1 << 3,
        [Description("dhlth")] // Health: disable checks
        HealthCheckDisable = 1 << 4,
        [Description("ehlth")] // Health: enable checks
        HealthCheckEnable = 1 << 5,
        [Description("hrunn")] // Health: force UP
        HealthForceUp = 1 << 6,
        [Description("hnolb")] // Health: force NOLB
        HealthForceNoLB = 1 << 7,
        [Description("hdown")] // Health: force DOWN
        HealthForceDown = 1 << 8,
        [Description("dagent")] // Agent: disable checks
        AgentCheckDisable = 1 << 9,
        [Description("eagent")] // Agent: enable checks
        AgentCheckEnable = 1 << 10,
        [Description("arunn")] // Agent: force UP
        AgentForceUp = 1 << 11,
        [Description("adown")] // Agent: force DOWN
        AgentForceDown = 1 << 12,
        [Description("shutdown")] // Kill Sessions
        KillSessions = 1 << 13,
        // ReSharper restore InconsistentNaming
    }
}