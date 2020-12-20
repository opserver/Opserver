using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Primitives;
using Opserver.Data;
using Opserver.Models;

namespace Opserver.Security
{
    public abstract class SecurityProvider
    {
        public abstract string ProviderName { get; }
        public abstract SecurityProviderFlowType FlowType { get; }
        protected SecuritySettings Settings { get; set; }
        public virtual bool IsConfigured => true;
        public readonly List<IPNet> InternalNetworks;

        protected SecurityProvider(SecuritySettings settings)
        {
            Settings = settings;
            InternalNetworks = new List<IPNet>();
            if (settings.InternalNetworks != null)
            {
                foreach (var n in settings.InternalNetworks)
                {
                    if ((n.CIDR.HasValue() || (n.IP.HasValue() && n.Subnet.IsNullOrEmpty())) && IPNet.TryParse(n.CIDR.IsNullOrEmptyReturn(n.IP), out var net))
                    {
                        InternalNetworks.Add(net);
                    }
                    else if (n.IP.HasValue() && n.Subnet.HasValue() && IPNet.TryParse(n.IP, n.Subnet, out net))
                    {
                        InternalNetworks.Add(net);
                    }
                }
            }
        }

        internal virtual bool InReadGroups(User user, StatusModule module)
        {
            var settings = module.SecuritySettings;
            // User has direct access, global access, or is an admin
            return settings != null && (InGroups(user, Parse(settings.ViewGroups))
                                        || InGroups(user, Parse(Settings.ViewEverythingGroups))
                                        || InAdminGroups(user, module));
        }

        // The theory here is that there won't be that many unqiue combinations here.
        private readonly ConcurrentDictionary<string, string[]> _parsedGroups = new ConcurrentDictionary<string, string[]>();
        private string[] Parse(string groups) =>
            groups.IsNullOrEmpty()
            ? Array.Empty<string>()
            : _parsedGroups.GetOrAdd(groups, groups => groups?.Split(StringSplits.Comma_SemiColon) ?? Array.Empty<string>());

        /// <summary>
        /// Checks if a request's API key is valid.
        /// </summary>
        /// <remarks>
        /// Tons to do here: roles, multiple keys, encryption, etc.
        /// </remarks>
        internal bool IsValidApiKey(StringValues? key) =>
            Settings.ApiKey.HasValue() && string.Equals(Settings.ApiKey, key);

        internal virtual bool InAdminGroups(User user, StatusModule module)
        {
            var settings = module.SecuritySettings;
            // User either has direct access or via global
            return settings != null && (InGroups(user, Parse(settings.AdminGroups)) || InGroups(user, Parse(Settings.AdminEverythingGroups)));
        }

        public virtual bool InGroups(User user, string[] groupNames) => false;
        public abstract bool ValidateUser(string userName, string password);

        public abstract bool ValidateToken(ISecurityProviderToken token);

        public bool IsGlobalAdmin(User user) => InGroups(user, Parse(Settings.AdminEverythingGroups));

        public virtual void PurgeCache() { }

        public bool IsInternalIP(string ip) =>
            IPAddress.TryParse(ip, out var addr) && InternalNetworks.Any(n => n.Contains(addr));
    }
}
