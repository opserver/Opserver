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
        protected SecuritySettings Settings { get; set; }
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
            return settings != null && (InGroups(user, settings.ViewGroups) || InAdminGroups(user, module));
        }

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
            return settings != null && InGroups(user, settings.AdminGroups);
        }

        public virtual bool InGroups(User user, string groupNames) => false;
        public abstract bool ValidateUser(string userName, string password);

        public virtual void PurgeCache() { }

        public bool IsInternalIP(string ip) =>
            IPAddress.TryParse(ip, out var addr) && InternalNetworks.Any(n => n.Contains(addr));
    }
}
