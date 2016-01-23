using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
            Parallel.ForEach(instances, i => i.PollAsync(true));
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
            if (action == Action.drain && server.ProxyServerStatus == ProxyServerStatus.Down)
            {
                action = Action.maint;
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
                        ["action"] = action.ToString().ToLower(),
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

    public enum Action
    {
        // ReSharper disable InconsistentNaming
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
        // ReSharper restore InconsistentNaming
    }
}