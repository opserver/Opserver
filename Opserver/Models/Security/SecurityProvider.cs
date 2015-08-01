using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Opserver.Data;

namespace StackExchange.Opserver.Models.Security
{
    public abstract class SecurityProvider
    {
        public virtual bool IsAdmin => InGroups(SiteSettings.AdminGroups);
        public virtual bool IsViewer => InGroups(SiteSettings.ViewGroups);

        internal virtual bool InReadGroups(ISecurableSection settings)
        {
            return IsViewer || (settings != null && (InGroups(settings.ViewGroups) || InAdminGroups(settings)));
        }

        internal virtual bool InAdminGroups(ISecurableSection settings)
        {
            return IsAdmin || (settings != null && InGroups(settings.AdminGroups));
        }
        
        private bool InGroups(string groupNames)
        {
            if (groupNames.IsNullOrEmpty() || Current.User.AccountName.IsNullOrEmpty()) return false;
            return groupNames == "*" || InGroups(groupNames, Current.User.AccountName);
        }
        
        public abstract bool InGroups(string groupNames, string accountName);
        public abstract bool ValidateUser(string userName, string password);

        public virtual List<string> GetGroupMembers(string groupName)
        {
            return new List<string>();
        }
        public virtual void PurgeCache() { }

        public List<IPNet> InternalNetworks;
        protected SecurityProvider()
        {
            InternalNetworks = new List<IPNet>();
            if (SecuritySettings.Current.InternalNetworks != null)
            {
                foreach (var n in SecuritySettings.Current.InternalNetworks.All)
                {
                    IPNet net;
                    if ((n.CIDR.HasValue() || (n.IP.HasValue() && n.Subnet.IsNullOrEmpty())) && IPNet.TryParse(n.CIDR.IsNullOrEmptyReturn(n.IP), out net))
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

        public bool IsInternalIP(string ip)
        {
            IPAddress addr;
            return IPAddress.TryParse(ip, out addr) && InternalNetworks.Any(n => n.Contains(addr));
        }
    }
}